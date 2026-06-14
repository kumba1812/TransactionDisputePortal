export const STATUS_MAP = {
  0: 'completed',
  1: 'pending',
  2: 'underreview',
  3: 'resolved',
  4: 'refunded',
  5: 'rejected',
};

export const DISPUTE_STATUS_MAP = {
  0: 'pending',
  1: 'underreview',
  2: 'resolved',
  3: 'refunded',
  4: 'rejected',
};

export const getStatusLabel = (status) => {
  if (typeof status === 'string') return status;
  return DISPUTE_STATUS_MAP[status] || 'unknown';
};

export const getTransactionStatusLabel = (status) => {
  if (typeof status === 'string') return status;
  return STATUS_MAP[status] || 'unknown';
};

// South African formatting
export const formatCurrency = (amount) => {
  return new Intl.NumberFormat('en-ZA', {
    style: 'currency',
    currency: 'ZAR',
  }).format(amount || 0);
};

export const formatDate = (dateString) => {
  return new Intl.DateTimeFormat('en-ZA', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date(dateString));
};
