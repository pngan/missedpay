import { useState, useEffect } from 'react';
import AccountCard from './components/AccountCard';
import TransactionList from './components/TransactionList';
import { accountsApi, transactionsApi } from './services/api';

function App() {
  const [accounts, setAccounts] = useState([]);
  const [transactions, setTransactions] = useState([]);
  const [selectedAccount, setSelectedAccount] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [accountsData, transactionsData] = await Promise.all([
        accountsApi.getAll(),
        transactionsApi.getAll(),
      ]);
      setAccounts(accountsData);
      setTransactions(transactionsData);
    } catch (err) {
      setError(err.message);
      console.error('Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAccountClick = (account) => {
    setSelectedAccount(selectedAccount?._id === account._id ? null : account);
  };

  const calculateTotalBalance = () => {
    return accounts.reduce((sum, account) => sum + (account.balance?.current || 0), 0);
  };

  const formatCurrency = (amount, currency = 'NZD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const getTransactionCount = (accountId) => {
    return transactions.filter(t => t._account === accountId).length;
  };

  if (loading) {
    return (
      <div style={{ 
        maxWidth: '1200px', 
        margin: '0 auto', 
        padding: '40px 20px',
        fontFamily: 'system-ui, -apple-system, sans-serif'
      }}>
        <p style={{ fontSize: '16px', color: '#666' }}>Loading your accounts...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ 
        maxWidth: '1200px', 
        margin: '0 auto', 
        padding: '40px 20px',
        fontFamily: 'system-ui, -apple-system, sans-serif'
      }}>
        <h2 style={{ fontSize: '24px', marginBottom: '12px', color: '#111' }}>Oops! Something went wrong</h2>
        <p style={{ fontSize: '16px', color: '#666', marginBottom: '20px' }}>{error}</p>
        <button 
          onClick={loadData}
          style={{
            padding: '10px 20px',
            fontSize: '14px',
            backgroundColor: '#111',
            color: '#fff',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer'
          }}
        >
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div style={{ 
      minHeight: '100vh',
      backgroundColor: '#f9fafb',
      fontFamily: 'system-ui, -apple-system, sans-serif'
    }}>
      {/* Header - visible on all screens */}
      <div style={{
        backgroundColor: '#fff',
        borderBottom: '1px solid #e5e7eb',
        padding: '20px',
        position: 'sticky',
        top: 0,
        zIndex: 10
      }}>
        <div style={{ 
          maxWidth: '1400px', 
          margin: '0 auto'
        }}>
          <h1 style={{ 
            fontSize: '24px', 
            fontWeight: '600', 
            marginBottom: '4px',
            color: '#111'
          }}>
            Bank Accounts
          </h1>
          <p style={{ 
            fontSize: '14px', 
            color: '#6b7280',
            margin: 0
          }}>
            Manage your accounts and view transaction history
          </p>
        </div>
      </div>

      {/* Main Content Area */}
      <div style={{
        maxWidth: '1400px',
        margin: '0 auto',
        padding: '20px',
        display: 'flex',
        gap: '24px',
        flexWrap: 'wrap'
      }}>
        {/* Left Panel - Accounts */}
        <div style={{
          flex: '1 1 400px',
          minWidth: '300px',
          maxWidth: selectedAccount ? '450px' : '100%'
        }}>
          {/* Total Balance Card */}
          <div style={{
            backgroundColor: '#fff',
            borderRadius: '12px',
            padding: '24px',
            marginBottom: '20px',
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
          }}>
            <p style={{ 
              fontSize: '14px', 
              color: '#6b7280',
              margin: '0 0 8px 0',
              fontWeight: '500'
            }}>
              Total Balance
            </p>
            <p style={{ 
              fontSize: '32px', 
              fontWeight: '600',
              margin: 0,
              color: '#111'
            }}>
              {formatCurrency(calculateTotalBalance())}
            </p>
          </div>

          {/* Accounts List */}
          <div style={{
            backgroundColor: '#fff',
            borderRadius: '12px',
            padding: '20px',
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
          }}>
            <h2 style={{
              fontSize: '18px',
              fontWeight: '600',
              color: '#111',
              marginBottom: '16px'
            }}>
              Your Accounts
            </h2>
            
            {accounts.length === 0 ? (
              <div style={{
                textAlign: 'center',
                padding: '40px 20px',
                backgroundColor: '#f9fafb',
                borderRadius: '8px'
              }}>
                <h3 style={{ fontSize: '16px', marginBottom: '8px', color: '#111' }}>No accounts found</h3>
                <p style={{ fontSize: '14px', color: '#6b7280', margin: 0 }}>Add your first account to get started</p>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                {accounts.map((account) => (
                  <AccountCard
                    key={account._id}
                    account={account}
                    isSelected={selectedAccount?._id === account._id}
                    onClick={handleAccountClick}
                    transactionCount={getTransactionCount(account._id)}
                  />
                ))}
              </div>
            )}

            {!selectedAccount && accounts.length > 0 && (
              <div style={{
                marginTop: '16px',
                padding: '12px',
                backgroundColor: '#f9fafb',
                borderRadius: '8px',
                textAlign: 'center'
              }}>
                <p style={{ 
                  fontSize: '13px', 
                  color: '#6b7280',
                  margin: 0
                }}>
                  Click on any account to view transactions
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Right Panel - Transactions */}
        {selectedAccount && (
          <div style={{
            flex: '1 1 600px',
            minWidth: '300px'
          }}>
            <div style={{
              backgroundColor: '#fff',
              borderRadius: '12px',
              padding: '20px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
            }}>
              <TransactionList
                transactions={transactions}
                accountId={selectedAccount._id}
                account={selectedAccount}
              />
            </div>
          </div>
        )}

        {/* Placeholder when no account selected - only on desktop */}
        {!selectedAccount && accounts.length > 0 && (
          <div style={{
            flex: '1 1 600px',
            minWidth: '300px',
            display: 'none',
            '@media (min-width: 768px)': {
              display: 'block'
            }
          }}>
            <div style={{
              backgroundColor: '#fff',
              borderRadius: '12px',
              padding: '60px 40px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
              textAlign: 'center',
              height: '100%',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center'
            }}>
              <div style={{
                fontSize: '64px',
                marginBottom: '16px',
                opacity: 0.3
              }}>
                💳
              </div>
              <h3 style={{ 
                fontSize: '20px', 
                fontWeight: '600',
                marginBottom: '8px', 
                color: '#111' 
              }}>
                Select an Account
              </h3>
              <p style={{ 
                fontSize: '14px', 
                color: '#6b7280',
                margin: 0
              }}>
                Choose an account from the left to view its transaction history
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
