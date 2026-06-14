import axios from 'axios';

const API_BASE_URL = 'http://localhost:5115/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const transactionApi = {
  getTransactions: () => apiClient.get('/transactions'),
  getTransaction: (id) => apiClient.get(`/transactions/${id}`),
  createTransaction: (data) => apiClient.post('/transactions', data),
  updateTransaction: (id, data) => apiClient.put(`/transactions/${id}`, data),
  deleteTransaction: (id) => apiClient.delete(`/transactions/${id}`),
};

export const disputeApi = {
  getDisputes: () => apiClient.get('/disputes'),
  getDispute: (id) => apiClient.get(`/disputes/${id}`),
  getDisputesByTransaction: (transactionId) => apiClient.get(`/disputes/transaction/${transactionId}`),
  createDispute: (data) => apiClient.post('/disputes', data),
  updateDispute: (id, data) => apiClient.put(`/disputes/${id}`, data),
  deleteDispute: (id) => apiClient.delete(`/disputes/${id}`),
};

export const healthApi = {
  check: () => apiClient.get('/health'),
};
