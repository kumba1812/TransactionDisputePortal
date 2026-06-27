import axios from 'axios';

const API_BASE_URL = 'http://localhost:5115/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Attach JWT from sessionStorage on every request
apiClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('tdp_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// On 401, clear auth and redirect to login (hard reload so AuthContext resets)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      sessionStorage.removeItem('tdp_token');
      sessionStorage.removeItem('tdp_user');
      window.location.href = '/';
    }
    return Promise.reject(error);
  }
);

export const transactionApi = {
  getTransactions: ()           => apiClient.get('/transactions'),
  getTransaction:  (id)         => apiClient.get(`/transactions/${id}`),
  createTransaction: (data)     => apiClient.post('/transactions', data),
  updateTransaction: (id, data) => apiClient.put(`/transactions/${id}`, data),
  deleteTransaction: (id)       => apiClient.delete(`/transactions/${id}`),
};

export const disputeApi = {
  getDisputes:              ()           => apiClient.get('/disputes'),
  getDispute:               (id)         => apiClient.get(`/disputes/${id}`),
  getDisputesByTransaction: (txnId)      => apiClient.get(`/disputes/transaction/${txnId}`),
  createDispute:            (data)       => apiClient.post('/disputes', data),
  updateDispute:            (id, data)   => apiClient.put(`/disputes/${id}`, data),
  deleteDispute:            (id)         => apiClient.delete(`/disputes/${id}`),
  acquireLock:              (id)         => apiClient.post(`/disputes/${id}/lock`),
  releaseLock:              (id)         => apiClient.delete(`/disputes/${id}/lock`),
};

export const healthApi = {
  check: () => apiClient.get('/health'),
};

