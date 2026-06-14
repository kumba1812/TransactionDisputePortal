import { useState } from 'react'
import './App.css'
import { TransactionsList } from './components/TransactionsList'
import { DisputeForm } from './components/DisputeForm'
import { DisputeHistory } from './components/DisputeHistory'

function App() {
  const [selectedTransaction, setSelectedTransaction] = useState(null)
  const [activeTab, setActiveTab] = useState('transactions')
  const [refreshKey, setRefreshKey] = useState(0)

  const handleDisputeCreated = () => {
    setSelectedTransaction(null)
    setActiveTab('history')
    setRefreshKey(prev => prev + 1)
  }

  return (
    <div className="app">
      <header className="app-header">
        <div className="header-content">
          <h1>Transaction Dispute Portal</h1>
          <p className="subtitle">Manage your transactions and disputes</p>
        </div>
      </header>

      <nav className="app-nav">
        <button 
          className={`nav-btn ${activeTab === 'transactions' ? 'active' : ''}`}
          onClick={() => setActiveTab('transactions')}
        >
          💳 Transactions
        </button>
        <button 
          className={`nav-btn ${activeTab === 'history' ? 'active' : ''}`}
          onClick={() => setActiveTab('history')}
        >
          📋 Dispute History
        </button>
      </nav>

      <main className="app-content">
        {activeTab === 'transactions' && (
          <div className="tab-content">
            {!selectedTransaction ? (
              <TransactionsList onSelectTransaction={setSelectedTransaction} />
            ) : (
              <DisputeForm 
                transaction={selectedTransaction}
                onDisputeCreated={handleDisputeCreated}
                onCancel={() => setSelectedTransaction(null)}
              />
            )}
          </div>
        )}

        {activeTab === 'history' && (
          <div className="tab-content">
            <DisputeHistory key={refreshKey} />
          </div>
        )}
      </main>

      <footer className="app-footer">
        <p>&copy; 2024 Transaction Dispute Portal. All rights reserved.</p>
      </footer>
    </div>
  )
}

export default App
