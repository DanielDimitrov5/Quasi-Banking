import { apiClient } from './client';
import type { Account, CreateAccountRequest } from '$lib/types';

export const accountService = {
    async createAccount(data: CreateAccountRequest): Promise<Account> {
        const response = await apiClient.post<Account>('/api/accounts', data);
        return response.data;
    },

    async getAccount(id: string): Promise<Account> {
        const response = await apiClient.get<Account>(`/api/accounts/${id}`);
        return response.data;
    },

    async getAccountsByOwner(ownerId: string): Promise<Account[]> {
        const response = await apiClient.get<Account[]>(`/api/accounts/owner/${ownerId}`);
        return response.data;
    },
};
