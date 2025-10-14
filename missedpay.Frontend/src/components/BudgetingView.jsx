import { useState, useMemo, useEffect, useRef } from 'react';
import CategoryPicker from './CategoryPicker';
import { categorizationApi } from '../services/api';

const BudgetingView = ({ transactions, onTransactionUpdate }) => {
  const [selectedGroup, setSelectedGroup] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [sortOrder, setSortOrder] = useState('date'); // 'date' or 'amount'
  const [categorizingTransaction, setCategorizingTransaction] = useState(null);
  const [categorizingInProgress, setCategorizingInProgress] = useState(null);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const pendingTransactionIdRef = useRef(null);

  // Effect to load pending transaction after refresh
  useEffect(() => {
    const pendingId = sessionStorage.getItem('pendingCategorizationId');
    
    if (!isRefreshing && pendingId && transactions) {
      sessionStorage.removeItem('pendingCategorizationId');
      
      // Build fresh groups from updated transactions
      const groups = {};
      let uncategorizedTransactions = [];
      
      transactions.forEach(transaction => {
        if (transaction.amount >= 0) return;
        
        const personalFinanceGroup = transaction.category?.groups?.personal_finance;
        
        if (!personalFinanceGroup) {
          uncategorizedTransactions.push(transaction);
          return;
        }
        
        const groupName = personalFinanceGroup.name;
        if (!groupName) return;

        if (!groups[groupName]) {
          groups[groupName] = {
            name: groupName,
            id: personalFinanceGroup._id,
            categories: {}
          };
        }

        const categoryName = transaction.category?.name;
        if (categoryName) {
          if (!groups[groupName].categories[categoryName]) {
            groups[groupName].categories[categoryName] = {
              name: categoryName,
              id: transaction.category._id,
              transactions: []
            };
          }
          groups[groupName].categories[categoryName].transactions.push(transaction);
        }
      });

      // Add uncategorized
      if (uncategorizedTransactions.length > 0) {
        groups['Uncategorized'] = {
          name: 'Uncategorized',
          id: 'uncategorized',
          categories: {
            'No category data': {
              name: 'No category data',
              id: 'no-category',
              transactions: uncategorizedTransactions
            }
          }
        };
      }
      
      // Find the transaction by ID
      let foundTransaction = null;
      
      for (const group of Object.values(groups)) {
        for (const category of Object.values(group.categories)) {
          foundTransaction = category.transactions.find(t => t._id === pendingId);
          if (foundTransaction) break;
        }
        if (foundTransaction) break;
      }
      
      if (foundTransaction) {
        setCategorizingTransaction(foundTransaction);
        setIsRefreshing(false);  // Clear refresh flag after loading
      } else {
        setCategorizingTransaction(null);
        setIsRefreshing(false);
      }
    }
  }, [isRefreshing, transactions]);

  const formatCurrency = (amount) => {
    return '$' + new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(Math.abs(amount));
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (date.toDateString() === yesterday.toDateString()) {
      return 'Yesterday';
    } else {
      return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }
  };

  // Aggregate transactions by personal_finance group
  const groupAggregates = useMemo(() => {
    const groups = {};
    let uncategorizedTotal = 0;
    let uncategorizedCount = 0;
    let uncategorizedTransactions = [];
    
    transactions.forEach(transaction => {
      // Only process expenses (negative amounts)
      if (transaction.amount >= 0) return;
      
      // Access the personal_finance group from the groups dictionary
      const personalFinanceGroup = transaction.category?.groups?.personal_finance;
      
      if (!personalFinanceGroup) {
        // Track uncategorized transactions
        uncategorizedTotal += Math.abs(transaction.amount);
        uncategorizedCount += 1;
        uncategorizedTransactions.push(transaction);
        return;
      }
      
      const groupName = personalFinanceGroup.name;
      const groupId = personalFinanceGroup._id;
      
      if (!groupName) return;

      if (!groups[groupName]) {
        groups[groupName] = {
          name: groupName,
          id: groupId,
          total: 0,
          count: 0,
          categories: {}
        };
      }

      groups[groupName].total += Math.abs(transaction.amount);
      groups[groupName].count += 1;

      // Aggregate by category within the group
      const categoryName = transaction.category?.name;
      if (categoryName) {
        if (!groups[groupName].categories[categoryName]) {
          groups[groupName].categories[categoryName] = {
            name: categoryName,
            id: transaction.category._id,
            total: 0,
            count: 0,
            transactions: []
          };
        }
        groups[groupName].categories[categoryName].total += Math.abs(transaction.amount);
        groups[groupName].categories[categoryName].count += 1;
        groups[groupName].categories[categoryName].transactions.push(transaction);
      }
    });

    // Add uncategorized as a group if there are any
    if (uncategorizedCount > 0) {
      groups['Uncategorized'] = {
        name: 'Uncategorized',
        id: 'uncategorized',
        total: uncategorizedTotal,
        count: uncategorizedCount,
        categories: {
          'No category data': {
            name: 'No category data',
            id: 'no-category',
            total: uncategorizedTotal,
            count: uncategorizedCount,
            transactions: uncategorizedTransactions
          }
        }
      };
    }
    
    // Convert to array and sort by total descending
    return Object.values(groups).sort((a, b) => b.total - a.total);
  }, [transactions]);

  const totalSpending = useMemo(() => {
    return groupAggregates.reduce((sum, group) => sum + group.total, 0);
  }, [groupAggregates]);

  const getGroupIcon = (groupName) => {
    const name = groupName.toLowerCase();
    if (name.includes('uncategorized')) return '❓';
    if (name.includes('food') || name.includes('dining')) return '🍔';
    if (name.includes('transport')) return '🚗';
    if (name.includes('utilities')) return '⚡';
    if (name.includes('entertainment')) return '🎬';
    if (name.includes('shopping')) return '🛍️';
    if (name.includes('health')) return '💊';
    if (name.includes('home')) return '🏠';
    if (name.includes('education')) return '📚';
    if (name.includes('personal')) return '👤';
    return '💳';
  };

  const getCategoryIcon = (categoryName) => {
    const name = categoryName.toLowerCase();
    if (name.includes('supermarket') || name.includes('grocery')) return '🛒';
    if (name.includes('restaurant') || name.includes('cafe')) return '☕';
    if (name.includes('telecom') || name.includes('phone')) return '📱';
    if (name.includes('electric') || name.includes('power')) return '💡';
    if (name.includes('gas') || name.includes('fuel')) return '⛽';
    if (name.includes('internet')) return '🌐';
    if (name.includes('water')) return '💧';
    return '📊';
  };

  const handleCategorySelect = async (category) => {
    if (!categorizingTransaction) return;
    
    const currentTransactionId = categorizingTransaction._id;
    const categorizedMerchantName = categorizingTransaction.merchant?.name || categorizingTransaction.description;
    
    setCategorizingInProgress(currentTransactionId);
    
    try {
      // Call the API to save the merchant→category mapping
      await categorizationApi.confirmCategory(
        categorizedMerchantName,
        category.id
      );
      
      // Find the next uncategorized transaction ID BEFORE refresh
      const nextTransactionId = findNextUncategorizedTransaction(currentTransactionId, categorizedMerchantName);
      
      if (nextTransactionId) {
        // Store the pending transaction ID in sessionStorage (survives component remounts)
        sessionStorage.setItem('pendingCategorizationId', nextTransactionId);
      } else {
        sessionStorage.removeItem('pendingCategorizationId');
      }
      
      // Set refreshing flag BEFORE calling onTransactionUpdate
      setIsRefreshing(true);
      
      // Notify parent to refresh transactions
      if (onTransactionUpdate) {
        await onTransactionUpdate();
      }
      
    } catch (error) {
      console.error('Failed to save category:', error);
      alert('Failed to save category. Please try again.');
      sessionStorage.removeItem('pendingCategorizationId');
      setCategorizingInProgress(null);
      setIsRefreshing(false);
    }
    
    // Clear in-progress state (useEffect will handle loading next transaction)
    setCategorizingInProgress(null);
  };

  const findNextUncategorizedTransaction = (currentTransactionId, categorizedMerchant) => {
    // Get all transactions from all categories (including uncategorized)
    const allTransactions = [];
    
    groupAggregates.forEach(group => {
      Object.values(group.categories).forEach(category => {
        category.transactions.forEach(transaction => {
          allTransactions.push(transaction);
        });
      });
    });
    
    // Filter to only uncategorized transactions (those without a category or in "Uncategorized" group)
    const uncategorizedTransactions = allTransactions.filter(t => {
      // Skip transactions from the merchant we just categorized (they'll be updated on next refresh)
      const merchantName = t.merchant?.name || t.description;
      if (merchantName === categorizedMerchant) {
        return false;
      }
      
      // Check if transaction is uncategorized
      const hasCategory = t.category && 
                         t.category.groups && 
                         t.category.groups.personal_finance &&
                         t.category.groups.personal_finance.name !== 'Uncategorized';
      
      return !hasCategory;
    });
    
    // Find the current transaction's index
    const currentIndex = uncategorizedTransactions.findIndex(t => t._id === currentTransactionId);
    
    // Return the ID of the next one, or the first one if current is not found or is the last
    if (currentIndex >= 0 && currentIndex < uncategorizedTransactions.length - 1) {
      return uncategorizedTransactions[currentIndex + 1]._id;
    } else if (uncategorizedTransactions.length > 0) {
      return uncategorizedTransactions[0]._id;
    }
    
    return null;
  };

  const countUncategorizedTransactions = () => {
    // Get all transactions from all categories
    const allTransactions = [];
    
    groupAggregates.forEach(group => {
      Object.values(group.categories).forEach(category => {
        category.transactions.forEach(transaction => {
          allTransactions.push(transaction);
        });
      });
    });
    
    // Count uncategorized transactions
    return allTransactions.filter(t => {
      const hasCategory = t.category && 
                         t.category.groups && 
                         t.category.groups.personal_finance &&
                         t.category.groups.personal_finance.name !== 'Uncategorized';
      
      return !hasCategory;
    }).length;
  };

  if (transactions.length === 0) {
    return (
      <div style={{
        maxWidth: '1400px',
        margin: '0 auto',
        padding: '20px'
      }}>
        <div style={{
          backgroundColor: '#fff',
          borderRadius: '12px',
          padding: '60px 40px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
          textAlign: 'center'
        }}>
          <div style={{
            fontSize: '64px',
            marginBottom: '16px',
            opacity: 0.3
          }}>
            📊
          </div>
          <h3 style={{ 
            fontSize: '20px', 
            fontWeight: '600',
            marginBottom: '8px', 
            color: '#111' 
          }}>
            No Transaction Data
          </h3>
          <p style={{ 
            fontSize: '14px', 
            color: '#6b7280',
            margin: 0
          }}>
            Import transactions from your bank accounts to see spending insights
          </p>
        </div>
      </div>
    );
  }

  return (
    <div style={{
      maxWidth: '1400px',
      margin: '0 auto',
      padding: '20px',
      display: 'flex',
      gap: '24px',
      flexWrap: 'wrap'
    }}>
      {/* Left Panel - Group Summary */}
      <div style={{
        flex: '1 1 400px',
        minWidth: '300px',
        maxWidth: selectedGroup ? '450px' : '100%'
      }}>
        {/* Total Spending Card */}
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
            Total Spending (Last 31 Days)
          </p>
          <p style={{ 
            fontSize: '32px', 
            fontWeight: '600',
            margin: 0,
            color: '#dc2626'
          }}>
            {formatCurrency(totalSpending)}
          </p>
          <p style={{ 
            fontSize: '13px', 
            color: '#9ca3af',
            margin: '8px 0 0 0'
          }}>
            Across {groupAggregates.length} categories
          </p>
        </div>

        {/* Spending Groups */}
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
            Spending by Category
          </h2>
          
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {groupAggregates.map((group) => {
              const percentage = (group.total / totalSpending * 100).toFixed(1);
              const isExpanded = selectedGroup?.id === group.id;
              
              return (
                <div
                  key={group.id}
                  style={{
                    backgroundColor: '#fff',
                    border: `1px solid ${isExpanded ? '#d1d5db' : '#e5e7eb'}`,
                    borderRadius: '8px',
                    overflow: 'hidden',
                    transition: 'all 0.2s ease'
                  }}
                >
                  {/* Group Header */}
                  <div
                    onClick={() => {
                      if (isExpanded) {
                        setSelectedGroup(null);
                        setSelectedCategory(null);
                      } else {
                        setSelectedGroup(group);
                        setSelectedCategory(null);
                      }
                    }}
                    style={{
                      padding: '12px 16px',
                      cursor: 'pointer',
                      backgroundColor: isExpanded ? '#f9fafb' : 'transparent',
                      transition: 'all 0.2s ease'
                    }}
                    onMouseEnter={(e) => {
                      if (!isExpanded) {
                        e.currentTarget.style.backgroundColor = '#f9fafb';
                      }
                    }}
                    onMouseLeave={(e) => {
                      if (!isExpanded) {
                        e.currentTarget.style.backgroundColor = 'transparent';
                      }
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                      <span style={{ fontSize: '24px' }}>{getGroupIcon(group.name)}</span>
                      
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'baseline',
                          marginBottom: '4px'
                        }}>
                          <span style={{
                            fontSize: '15px',
                            fontWeight: '600',
                            color: '#111'
                          }}>
                            {group.name}
                          </span>
                          <span style={{
                            fontSize: '15px',
                            fontWeight: '600',
                            color: '#dc2626'
                          }}>
                            {formatCurrency(group.total)}
                          </span>
                        </div>
                        
                        <div style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          gap: '8px'
                        }}>
                          <div style={{
                            flex: 1,
                            height: '4px',
                            backgroundColor: '#e5e7eb',
                            borderRadius: '2px',
                            overflow: 'hidden'
                          }}>
                            <div style={{
                              width: `${percentage}%`,
                              height: '100%',
                              backgroundColor: '#dc2626',
                              transition: 'width 0.3s ease'
                            }} />
                          </div>
                          <span style={{
                            fontSize: '12px',
                            color: '#6b7280',
                            minWidth: '40px',
                            textAlign: 'right'
                          }}>
                            {percentage}%
                          </span>
                        </div>
                        
                        <p style={{
                          fontSize: '12px',
                          color: '#9ca3af',
                          margin: '4px 0 0 0'
                        }}>
                          {group.count} transaction{group.count !== 1 ? 's' : ''}
                        </p>
                      </div>
                      
                      <span style={{
                        fontSize: '20px',
                        color: '#9ca3af',
                        transition: 'transform 0.2s ease',
                        transform: isExpanded ? 'rotate(180deg)' : 'rotate(0deg)'
                      }}>
                        ▼
                      </span>
                    </div>
                  </div>

                  {/* Accordion Content - Categories */}
                  {isExpanded && (
                    <div style={{
                      borderTop: '1px solid #e5e7eb',
                      padding: '12px',
                      backgroundColor: '#f9fafb'
                    }}>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                        {Object.values(group.categories)
                          .sort((a, b) => b.total - a.total)
                          .map((category) => {
                            const catPercentage = (category.total / group.total * 100).toFixed(1);
                            const isSelected = selectedCategory?.id === category.id;
                            
                            return (
                              <div
                                key={category.id}
                                onClick={(e) => {
                                  e.stopPropagation();
                                  setSelectedCategory(isSelected ? null : category);
                                }}
                                style={{
                                  backgroundColor: isSelected ? '#fff' : '#f3f4f6',
                                  border: `1px solid ${isSelected ? '#3b82f6' : 'transparent'}`,
                                  borderRadius: '6px',
                                  padding: '10px 12px',
                                  cursor: 'pointer',
                                  transition: 'all 0.2s ease'
                                }}
                                onMouseEnter={(e) => {
                                  if (!isSelected) {
                                    e.currentTarget.style.backgroundColor = '#fff';
                                  }
                                }}
                                onMouseLeave={(e) => {
                                  if (!isSelected) {
                                    e.currentTarget.style.backgroundColor = '#f3f4f6';
                                  }
                                }}
                              >
                                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                                  <span style={{ fontSize: '18px' }}>{getCategoryIcon(category.name)}</span>
                                  
                                  <div style={{ flex: 1, minWidth: 0 }}>
                                    <div style={{
                                      display: 'flex',
                                      justifyContent: 'space-between',
                                      alignItems: 'baseline',
                                      marginBottom: '4px'
                                    }}>
                                      <span style={{
                                        fontSize: '13px',
                                        fontWeight: '500',
                                        color: '#111',
                                        overflow: 'hidden',
                                        textOverflow: 'ellipsis',
                                        whiteSpace: 'nowrap'
                                      }}>
                                        {category.name}
                                      </span>
                                      <span style={{
                                        fontSize: '13px',
                                        fontWeight: '600',
                                        color: '#dc2626',
                                        marginLeft: '8px'
                                      }}>
                                        {formatCurrency(category.total)}
                                      </span>
                                    </div>
                                    
                                    <div style={{
                                      display: 'flex',
                                      justifyContent: 'space-between',
                                      alignItems: 'center',
                                      gap: '8px'
                                    }}>
                                      <div style={{
                                        flex: 1,
                                        height: '3px',
                                        backgroundColor: '#e5e7eb',
                                        borderRadius: '2px',
                                        overflow: 'hidden'
                                      }}>
                                        <div style={{
                                          width: `${catPercentage}%`,
                                          height: '100%',
                                          backgroundColor: '#f87171',
                                          transition: 'width 0.3s ease'
                                        }} />
                                      </div>
                                      <span style={{
                                        fontSize: '10px',
                                        color: '#9ca3af',
                                        minWidth: '35px',
                                        textAlign: 'right'
                                      }}>
                                        {catPercentage}%
                                      </span>
                                    </div>
                                    
                                    <p style={{
                                      fontSize: '10px',
                                      color: '#9ca3af',
                                      margin: '3px 0 0 0'
                                    }}>
                                      {category.count} transaction{category.count !== 1 ? 's' : ''}
                                    </p>
                                  </div>
                                </div>
                              </div>
                            );
                          })}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Right Panel - Transaction List */}
      {selectedCategory && (
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
            <div style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: '20px'
            }}>
              <div>
                <h2 style={{
                  fontSize: '20px',
                  fontWeight: '600',
                  color: '#111',
                  marginBottom: '4px'
                }}>
                  {selectedCategory.name}
                </h2>
                <p style={{
                  fontSize: '14px',
                  color: '#6b7280',
                  margin: 0
                }}>
                  {selectedCategory.count} transaction{selectedCategory.count !== 1 ? 's' : ''} • {formatCurrency(selectedCategory.total)}
                </p>
              </div>
              <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                {/* Sort Order Toggle */}
                <div style={{
                  display: 'flex',
                  gap: '4px',
                  padding: '4px',
                  backgroundColor: '#f3f4f6',
                  borderRadius: '6px'
                }}>
                  <button
                    onClick={() => setSortOrder('date')}
                    style={{
                      padding: '6px 12px',
                      fontSize: '13px',
                      fontWeight: '500',
                      backgroundColor: sortOrder === 'date' ? '#fff' : 'transparent',
                      color: sortOrder === 'date' ? '#111' : '#6b7280',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      transition: 'all 0.2s',
                      boxShadow: sortOrder === 'date' ? '0 1px 2px rgba(0, 0, 0, 0.05)' : 'none'
                    }}
                  >
                    📅 Date
                  </button>
                  <button
                    onClick={() => setSortOrder('amount')}
                    style={{
                      padding: '6px 12px',
                      fontSize: '13px',
                      fontWeight: '500',
                      backgroundColor: sortOrder === 'amount' ? '#fff' : 'transparent',
                      color: sortOrder === 'amount' ? '#111' : '#6b7280',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      transition: 'all 0.2s',
                      boxShadow: sortOrder === 'amount' ? '0 1px 2px rgba(0, 0, 0, 0.05)' : 'none'
                    }}
                  >
                    💰 Amount
                  </button>
                </div>
                <button
                  onClick={() => setSelectedCategory(null)}
                  style={{
                    padding: '8px 12px',
                    fontSize: '14px',
                    backgroundColor: 'transparent',
                    color: '#6b7280',
                    border: '1px solid #e5e7eb',
                    borderRadius: '6px',
                    cursor: 'pointer',
                    transition: 'all 0.2s'
                  }}
                  onMouseOver={(e) => {
                    e.currentTarget.style.backgroundColor = '#f3f4f6';
                  }}
                  onMouseOut={(e) => {
                    e.currentTarget.style.backgroundColor = 'transparent';
                  }}
                >
                  Close
                </button>
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {selectedCategory.transactions
                .sort((a, b) => {
                  if (sortOrder === 'date') {
                    return new Date(b.date) - new Date(a.date);
                  } else {
                    return Math.abs(b.amount) - Math.abs(a.amount);
                  }
                })
                .map((transaction) => (
                  <div
                    key={transaction._id}
                    style={{
                      backgroundColor: '#fff',
                      border: '1px solid #e5e7eb',
                      borderRadius: '8px',
                      padding: '12px 16px',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '12px',
                      transition: 'all 0.2s ease'
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = '#f9fafb';
                      e.currentTarget.style.boxShadow = '0 2px 4px rgba(0, 0, 0, 0.05)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = '#fff';
                      e.currentTarget.style.boxShadow = 'none';
                    }}
                  >
                    {/* Merchant Logo */}
                    {transaction.merchant?.logo && (
                      <img
                        src={transaction.merchant.logo}
                        alt={transaction.merchant.name}
                        style={{
                          width: '32px',
                          height: '32px',
                          borderRadius: '6px',
                          objectFit: 'cover'
                        }}
                      />
                    )}
                    {!transaction.merchant?.logo && (
                      <div style={{
                        width: '32px',
                        height: '32px',
                        borderRadius: '6px',
                        backgroundColor: '#f3f4f6',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontSize: '16px'
                      }}>
                        💳
                      </div>
                    )}

                    {/* Transaction Details */}
                    <div style={{ flex: 1, minWidth: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
                      {/* Description */}
                      <span style={{
                        fontSize: '14px',
                        fontWeight: '500',
                        color: '#111',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                        flex: '1 1 auto',
                        minWidth: '100px'
                      }}>
                        {transaction.description}
                      </span>

                      {/* Date */}
                      <span style={{
                        fontSize: '12px',
                        color: '#9ca3af',
                        whiteSpace: 'nowrap',
                        flex: '0 0 auto'
                      }}>
                        {formatDate(transaction.date)}
                      </span>
                    </div>

                    {/* Amount */}
                    <span style={{
                      fontSize: '14px',
                      fontWeight: '600',
                      color: transaction.amount < 0 ? '#dc2626' : '#059669',
                      whiteSpace: 'nowrap',
                      flex: '0 0 auto'
                    }}>
                      {formatCurrency(transaction.amount)}
                    </span>

                    {/* Categorize Button */}
                    <button
                      onClick={() => setCategorizingTransaction(transaction)}
                      disabled={categorizingInProgress === transaction._id}
                      style={{
                        padding: '6px 12px',
                        fontSize: '12px',
                        fontWeight: '500',
                        color: '#3b82f6',
                        backgroundColor: 'transparent',
                        border: '1px solid #3b82f6',
                        borderRadius: '6px',
                        cursor: categorizingInProgress === transaction._id ? 'not-allowed' : 'pointer',
                        whiteSpace: 'nowrap',
                        flex: '0 0 auto',
                        opacity: categorizingInProgress === transaction._id ? 0.5 : 1,
                        transition: 'all 0.2s ease'
                      }}
                      onMouseEnter={(e) => {
                        if (categorizingInProgress !== transaction._id) {
                          e.currentTarget.style.backgroundColor = '#3b82f6';
                          e.currentTarget.style.color = '#fff';
                        }
                      }}
                      onMouseLeave={(e) => {
                        e.currentTarget.style.backgroundColor = 'transparent';
                        e.currentTarget.style.color = '#3b82f6';
                      }}
                    >
                      {categorizingInProgress === transaction._id ? '...' : '📝 Categorize'}
                    </button>
                  </div>
                ))}
            </div>
          </div>
        </div>
      )}

      {/* Placeholder when no category selected */}
      {!selectedCategory && (
        <div style={{
          flex: '1 1 600px',
          minWidth: '300px'
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
              📊
            </div>
            <h3 style={{ 
              fontSize: '20px', 
              fontWeight: '600',
              marginBottom: '8px', 
              color: '#111' 
            }}>
              Select a Category
            </h3>
            <p style={{ 
              fontSize: '14px', 
              color: '#6b7280',
              margin: 0
            }}>
              Expand a spending group and click on a category to view transactions
            </p>
          </div>
        </div>
      )}

      {/* Category Picker Modal */}
      <CategoryPicker
        isOpen={categorizingTransaction !== null}
        onClose={() => setCategorizingTransaction(null)}
        onSelect={handleCategorySelect}
        merchantName={categorizingTransaction?.merchant?.name || categorizingTransaction?.description}
        currentCategory={categorizingTransaction?.category ? {
          name: categorizingTransaction.category.name,
          groupName: categorizingTransaction.category.groups?.personal_finance?.name
        } : null}
        remainingCount={countUncategorizedTransactions()}
      />
    </div>
  );
};

export default BudgetingView;
