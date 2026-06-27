// Shared fixture data for frontend tests

export const mockTransactions = [
  {
    id: 1,
    customerId: 4,
    transactionId: 'TX-001',
    amount: 150.0,
    description: 'Grocery purchase',
    transactionDate: '2026-06-01T10:00:00Z',
    merchant: 'Pick n Pay',
    category: 'Groceries',
    status: 0,
    createdAt: '2026-06-01T10:00:00Z',
    disputes: [],
  },
  {
    id: 2,
    customerId: 4,
    transactionId: 'TX-002',
    amount: 999.99,
    description: 'Electronics',
    transactionDate: '2026-06-10T14:30:00Z',
    merchant: 'Game',
    category: 'Electronics',
    status: 0,
    createdAt: '2026-06-10T14:30:00Z',
    disputes: [],
  },
]

export const mockDisputes = [
  {
    id: 1,
    transactionId: 1,
    transactionIdFk: 1,
    customerId: 4,
    reason: 'Fraud',
    description: 'I did not make this purchase',
    status: 0,
    createdAt: '2026-06-15T09:00:00Z',
    resolvedAt: null,
    resolutionNotes: null,
    refundAmount: 150.0,
    isLocked: false,
    lockedByName: null,
    lockedAt: null,
  },
  {
    id: 2,
    transactionId: 2,
    transactionIdFk: 2,
    customerId: 4,
    reason: 'Wrong amount',
    description: 'Charged twice',
    status: 1,
    createdAt: '2026-06-16T11:00:00Z',
    resolvedAt: null,
    resolutionNotes: null,
    refundAmount: 999.99,
    isLocked: true,
    lockedByName: 'Banker One',
    lockedAt: new Date(Date.now() - 3 * 60 * 1000).toISOString(), // 3 min ago
  },
]

export const mockUsers = {
  admin:   { id: 1, username: 'admin',   fullName: 'Admin User',   role: 'Admin' },
  banker:  { id: 2, username: 'banker',  fullName: 'Banker One',   role: 'Banker' },
  banker2: { id: 3, username: 'banker2', fullName: 'Banker Two',   role: 'Banker' },
  client:  { id: 4, username: 'client',  fullName: 'Client User',  role: 'Client' },
  readonly:{ id: 5, username: 'readonly',fullName: 'ReadOnly User',role: 'ReadOnly' },
}
