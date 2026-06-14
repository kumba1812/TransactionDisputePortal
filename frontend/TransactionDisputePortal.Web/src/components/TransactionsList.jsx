import { useState, useEffect } from 'react';
import { transactionApi, disputeApi } from '../services/api';
import '../styles/TransactionsList.css';

export function TransactionsList({ onSelectTransaction }) {
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchTransactions();
  }, []);

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const response = await transactionApi.getTransactions();
      setTransactions(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load transactions');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="loading">Loading transactions...</div>;
  if (error) return <div className="error">{error}</div>;

  return (
    <div className="transactions-list">
      <div className="list-header">
        <h2>Recent Transactions</h2>
        <button className="refresh-btn" onClick={fetchTransactions}>↻ Refresh</button>
      </div>
      {transactions.length === 0 ? (
        <p className="no-data">No transactions found</p>
      ) : (
        <div className="table-container">
          <table className="transactions-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Merchant</th>
                <th>Amount</th>
                <th>Category</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((transaction) => (
                <tr key={transaction.id}>
                  <td>{new Date(transaction.transactionDate).toLocaleDateString()}</td>
                  <td>{transaction.merchant}</td>
                  <td className="amount">${transaction.amount.toFixed(2)}</td>
                  <td>{transaction.category}</td>
                  <td>
                    <span className={`status status-${transaction.status.toLowerCase()}`}>
                      {transaction.status}
                    </span>
                  </td>
                  <td>
                    <button 
                      className="dispute-btn"
                      onClick={() => onSelectTransaction(transaction)}
                    >
                      View/Dispute
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
