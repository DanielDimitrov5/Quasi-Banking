import axios from 'axios';

const API_BASE_URL = import.meta.env.PUBLIC_API_URL || 'http://localhost:5050';

export const apiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 10000,
});

// Response interceptor for error handling
apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.data?.error) {
            throw new Error(error.response.data.error);
        }
        throw error;
    }
);
