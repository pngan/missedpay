import { useEffect, useState } from 'react';
import { categorizationApi } from '../services/api';
import './CategoryPicker.css';

/**
 * CategoryPicker component for selecting transaction categories
 * Features:
 * - Single-depth flat list with "Category - Group" pairs
 * - Keyboard shortcuts (1-9 for quick selection)
 * - ESC to close
 * - Shows merchant name being categorized
 * - Auto-advances to next uncategorized transaction
 */
const CategoryPicker = ({ isOpen, onClose, onSelect, merchantName, currentCategory, remainingCount }) => {
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(null);

  useEffect(() => {
    if (isOpen) {
      setLoading(true);
      setError(null);
      setSearchTerm('');
      setSelectedIndex(null);
      
      categorizationApi.getAllCategories()
        .then(data => {
          // Flatten grouped categories to single-depth list
          const flatList = [];
          Object.entries(data).forEach(([groupName, cats]) => {
            cats.forEach(cat => {
              flatList.push({
                id: cat._id,  // API returns _id, not id
                name: cat.name,
                groupName: groupName,
                display: `${cat.name} - ${groupName}`,
              });
            });
          });
          
          // Sort alphabetically by display name
          flatList.sort((a, b) => a.display.localeCompare(b.display));
          
          setCategories(flatList);
          setLoading(false);
        })
        .catch(err => {
          console.error('Failed to load categories:', err);
          setError('Failed to load categories. Please try again.');
          setLoading(false);
        });
    }
  }, [isOpen]);

  useEffect(() => {
    const handleKeyPress = (e) => {
      if (!isOpen) return;
      
      // ESC to close
      if (e.key === 'Escape') {
        onClose();
        return;
      }
      
      // Number keys 1-9 for quick selection
      const num = parseInt(e.key);
      if (num >= 1 && num <= 9) {
        // Prevent the number from being typed in the search field
        e.preventDefault();
        
        const filteredCategories = getFilteredCategories();
        if (filteredCategories[num - 1]) {
          // Show visual feedback
          setSelectedIndex(num - 1);
          
          // Brief delay to show selection, then trigger onSelect
          setTimeout(() => {
            onSelect(filteredCategories[num - 1]);
            setSelectedIndex(null);
          }, 150);
        }
      }
    };
    
    if (isOpen) {
      // Use keydown instead of keypress to catch the event before it reaches the input
      window.addEventListener('keydown', handleKeyPress);
      return () => window.removeEventListener('keydown', handleKeyPress);
    }
  }, [isOpen, categories, searchTerm, onClose, onSelect]);

  const getFilteredCategories = () => {
    if (!searchTerm) return categories;
    
    const term = searchTerm.toLowerCase();
    return categories.filter(cat => 
      cat.display.toLowerCase().includes(term)
    );
  };

  if (!isOpen) return null;

  const filteredCategories = getFilteredCategories();

  return (
    <div className="category-picker-overlay" onClick={onClose}>
      <div className="category-picker-modal" onClick={(e) => e.stopPropagation()}>
        <div className="category-picker-header">
          <div>
            <h3>Categorize Transaction</h3>
            {remainingCount > 0 && (
              <p style={{ 
                margin: '4px 0 0 0', 
                fontSize: '13px', 
                color: '#6b7280',
                fontWeight: 'normal'
              }}>
                {remainingCount} uncategorized transaction{remainingCount !== 1 ? 's' : ''} remaining
              </p>
            )}
          </div>
          <button className="close-button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>
        
        {merchantName && (
          <div className="merchant-info">
            <strong>Merchant:</strong> {merchantName}
          </div>
        )}
        
        {currentCategory && (
          <div className="current-category">
            <strong>Current:</strong> {currentCategory.name} - {currentCategory.groupName}
          </div>
        )}
        
        <div className="search-box">
          <input
            type="text"
            placeholder="Search categories..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            autoFocus
          />
        </div>

        <div className="category-list-container">
          {loading && (
            <div className="loading-state">
              <div className="spinner"></div>
              <p>Loading categories...</p>
            </div>
          )}
          
          {error && (
            <div className="error-state">
              <p>{error}</p>
              <button onClick={() => window.location.reload()}>Reload</button>
            </div>
          )}
          
          {!loading && !error && filteredCategories.length === 0 && (
            <div className="empty-state">
              <p>No categories found matching "{searchTerm}"</p>
            </div>
          )}
          
          {!loading && !error && filteredCategories.length > 0 && (
            <ul className="category-list">
              {filteredCategories.map((category, index) => (
                <li
                  key={category.id}
                  className={`category-item ${currentCategory?.id === category.id ? 'current' : ''} ${selectedIndex === index ? 'selecting' : ''}`}
                  onClick={() => onSelect(category)}
                  data-category-id={category.id}
                >
                  {index < 9 && (
                    <span className="keyboard-shortcut">{index + 1}</span>
                  )}
                  <span className="category-display">{category.display}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
        
        <div className="category-picker-footer">
          <small>
            Use number keys 1-9 for quick selection • ESC to close
            {remainingCount > 1 && ' • Auto-advances to next transaction'}
          </small>
        </div>
      </div>
    </div>
  );
};

export default CategoryPicker;
