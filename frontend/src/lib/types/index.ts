export interface Account {
    id: string;
    ownerId: string;
    ownerEmail: string;
    balance: number;
    createdAt: string;
}

export interface CreateAccountRequest {
    ownerId: string;
    ownerEmail: string;
    initialDeposit: number;
}

export interface Transaction {
    id: string;
    accountId: string;
    amount: number;
    type: 'Deposit' | 'Withdrawal';
    description: string;
    status: string;
    createdAt: string;
}

export interface TransactionRequest {
    accountId: string;
    amount: number;
    description: string;
}
