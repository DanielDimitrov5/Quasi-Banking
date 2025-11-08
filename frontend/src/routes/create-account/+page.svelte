<script lang="ts">
    import { goto } from '$app/navigation';
    import { accountService } from '$lib/api/accountService';
    import { toast } from '$lib/stores/toast';

    let ownerId = '';
    let ownerEmail = '';
    let initialDeposit = 0;
    let loading = false;
    let error = '';

    async function handleSubmit() {
        loading = true;
        error = '';

        try {
            const account = await accountService.createAccount({
                ownerId,
                ownerEmail,
                initialDeposit,
            });

            toast.show(
                `Account created successfully! Account ID: ${account.id}`,
                'success',
                6000
            );

            // Wait a bit before navigating to let user see the toast
            setTimeout(() => {
                goto('/');
            }, 1500);
        } catch (err: any) {
            error = err.message || 'Failed to create account';
            toast.show(error, 'error');
        } finally {
            loading = false;
        }
    }
</script>

<svelte:head>
    <title>Create Account - Banking App</title>
</svelte:head>

<div class="card">
    <h2>Create New Account</h2>

    {#if error}
        <div class="alert alert-error">{error}</div>
    {/if}

    <form on:submit|preventDefault={handleSubmit}>
        <div class="form-group">
            <label for="ownerId">Owner ID</label>
            <input
                type="text"
                id="ownerId"
                bind:value={ownerId}
                required
                placeholder="e.g., user-123"
            />
        </div>

        <div class="form-group">
            <label for="ownerEmail">Email</label>
            <input
                type="email"
                id="ownerEmail"
                bind:value={ownerEmail}
                required
                placeholder="user@example.com"
            />
        </div>

        <div class="form-group">
            <label for="initialDeposit">Initial Deposit</label>
            <input
                type="number"
                id="initialDeposit"
                bind:value={initialDeposit}
                min="0"
                step="0.01"
                required
                placeholder="1000.00"
            />
        </div>

        <button type="submit" class="btn btn-primary" disabled={loading}>
            {loading ? 'Creating...' : 'Create Account'}
        </button>
    </form>
</div>
