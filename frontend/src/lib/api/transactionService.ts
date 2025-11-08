import { apiClient } from './client';
import type { Transaction, TransactionRequest } from '$lib/types';

export const transactionService = {
    async deposit(data: TransactionRequest): Promise<Transaction> {
        const response = await apiClient.post<Transaction>('/api/transactions/deposit', data);
        return response.data;
    },

    async withdraw(data: TransactionRequest): Promise<Transaction> {
        const response = await apiClient.post<Transaction>('/api/transactions/withdraw', data);
        return response.data;
    },

    async getTransactionsByAccount(accountId: string): Promise<Transaction[]> {
        const response = await apiClient.get<Transaction[]>(
            `/api/transactions/account/${accountId}`
        );
        return response.data;
    },
};
