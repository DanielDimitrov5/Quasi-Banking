<script lang="ts">
    import { accountService } from '$lib/api/accountService';
    import { transactionService } from '$lib/api/transactionService';
    import { toast } from '$lib/stores/toast';
    import type { Account, Transaction } from '$lib/types';

    let accountId = '';
    let account: Account | null = null;
    let transactions: Transaction[] = [];
    let amount = '';
    let description = '';
    let loading = { account: false, transaction: false };

    async function loadAccount(isTransactionReload = false) {
        if (!accountId) return;

        if (!isTransactionReload) loading.account = true;

        try {
            account = await accountService.getAccount(accountId);
            await loadTransactions();
            if (!isTransactionReload) toast.show('Account loaded successfully', 'success');
        } catch (err: any) {
            toast.show(err.message || 'Failed to load account', 'error');
            account = null;
        } finally {
            loading.account = false;
        }
    }

    async function loadTransactions() {
        if (!accountId) return;

        try {
            transactions = await transactionService.getTransactionsByAccount(accountId);
        } catch (err: any) {
            console.error('Failed to load transactions:', err);
        }
    }

    async function handleDeposit() {
        if (!accountId || !amount || !account) return;

        loading.transaction = true;
        const depositAmount = parseFloat(amount);
        const previousBalance = account.balance;

        try {
            // Optimistically update UI immediately
            account.balance += depositAmount;

            await transactionService.deposit({
                accountId,
                amount: depositAmount,
                description: description || 'Deposit',
            });

            toast.show(`Deposit of $${depositAmount.toFixed(2)} successful!`, 'success');
            amount = '';
            description = '';

            // Poll for actual balance update (with exponential backoff)
            await pollForBalanceUpdate(accountId, previousBalance + depositAmount);
        } catch (err: any) {
            // Revert optimistic update on error
            account.balance = previousBalance;
            toast.show(err.message || 'Deposit failed', 'error');
        } finally {
            loading.transaction = false;
        }
    }

    async function handleWithdraw() {
        if (!accountId || !amount || !account) return;

        loading.transaction = true;
        const withdrawAmount = parseFloat(amount);
        const previousBalance = account.balance;

        try {
            // Optimistically update UI immediately
            account.balance -= withdrawAmount;

            await transactionService.withdraw({
                accountId,
                amount: withdrawAmount,
                description: description || 'Withdrawal',
            });

            toast.show(`Withdrawal of $${withdrawAmount.toFixed(2)} successful!`, 'success');
            amount = '';
            description = '';

            // Poll for actual balance update (with exponential backoff)
            await pollForBalanceUpdate(accountId, previousBalance - withdrawAmount);
        } catch (err: any) {
            // Revert optimistic update on error
            account.balance = previousBalance;
            toast.show(err.message || 'Withdrawal failed', 'error');
        } finally {
            loading.transaction = false;
        }
    }

    /**
     * Poll the account service to verify the balance was updated by the event consumer
     * Uses exponential backoff to reduce server load
     */
    async function pollForBalanceUpdate(accountId: string, expectedBalance: number, maxAttempts: number = 10) {
        let attempts = 0;
        const baseDelay = 500; // Start with 500ms

        while (attempts < maxAttempts) {
            await new Promise((resolve) => setTimeout(resolve, baseDelay * Math.pow(1.5, attempts)));

            try {
                const updatedAccount = await accountService.getAccount(accountId);

                // Check if balance matches expected value (with small tolerance for floating point)
                if (Math.abs(updatedAccount.balance - expectedBalance) < 0.01) {
                    // Balance confirmed, update UI with actual data
                    account = updatedAccount;
                    await loadTransactions();
                    console.log('✅ Balance confirmed after', attempts + 1, 'attempts');
                    return;
                }
            } catch (err) {
                console.warn('Failed to poll account balance:', err);
            }

            attempts++;
        }

        // If we reach here, polling timed out - do a final refresh
        console.warn('⚠️ Balance update polling timed out, doing final refresh');
        toast.show('Syncing balance...', 'info', 2000);
        await loadAccount(true);
    }

    function formatCurrency(value: number): string {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD',
        }).format(value);
    }

    function formatDate(dateString: string): string {
        return new Date(dateString).toLocaleString();
    }
</script>

<svelte:head>
    <title>Banking Dashboard</title>
</svelte:head>

<div class="card">
    <h2>Account Dashboard</h2>

    <div class="form-group">
        <label for="accountId">Account ID</label>
        <input
            type="text"
            id="accountId"
            bind:value={accountId}
            placeholder="Enter account ID"
            on:input={() => {
                account = null;
                transactions = [];
            }}
        />
    </div>

    <button
        class="btn btn-primary"
        on:click={() => loadAccount(false)}
        disabled={loading.account || !accountId}
    >
        {loading.account ? 'Loading...' : 'Load Account'}
    </button>

    {#if account}
        <div class="balance-display">{formatCurrency(account.balance)}</div>
        <p><strong>Owner:</strong> {account.ownerEmail}</p>
        <p><strong>Account ID:</strong> {account.id}</p>
        <p><strong>Created:</strong> {formatDate(account.createdAt)}</p>
    {/if}
</div>

{#if account}
    <div class="card">
        <h2>New Transaction</h2>

        <div class="form-group">
            <label for="amount">Amount</label>
            <input
                type="number"
                id="amount"
                bind:value={amount}
                placeholder="0.00"
                step="0.01"
                min="0"
            />
        </div>

        <div class="form-group">
            <label for="description">Description (Optional)</label>
            <input
                type="text"
                id="description"
                bind:value={description}
                placeholder="Transaction description"
            />
        </div>

        <div class="button-group">
            <button
                class="btn btn-success"
                on:click={handleDeposit}
                disabled={loading.transaction || !amount}
            >
                {loading.transaction ? 'Processing...' : 'Deposit'}
            </button>
            <button
                class="btn btn-danger"
                on:click={handleWithdraw}
                disabled={loading.transaction || !amount}
            >
                {loading.transaction ? 'Processing...' : 'Withdraw'}
            </button>
        </div>
    </div>

    <div class="card">
        <h2>Transaction History</h2>

        {#if transactions.length === 0}
            <p>No transactions yet</p>
        {:else}
            <div class="transaction-list">
                {#each transactions as tx}
                    <div class="transaction-item">
                        <div>
                            <div><strong>{tx.description}</strong></div>
                            <div style="font-size: 0.9rem; color: #7f8c8d;">
                                {formatDate(tx.createdAt)}
                            </div>
                        </div>
                        <div
                            class="transaction-amount"
                            class:positive={tx.amount >= 0}
                            class:negative={tx.amount < 0}
                        >
                            {tx.amount >= 0 ? '+' : ''}{formatCurrency(Math.abs(tx.amount))}
                        </div>
                    </div>
                {/each}
            </div>
        {/if}
    </div>
{/if}
