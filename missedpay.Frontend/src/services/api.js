const API_BASE_URL = 'http://localhost:5349/api';
const TENANT_ID = '01927b5e-8f3a-7000-8000-000000000000';

const headers = {
  'Content-Type': 'application/json',
  'X-Tenant-Id': TENANT_ID,
};

export const accountsApi = {
  async getAll() {
    const response = await fetch(`${API_BASE_URL}/Account`, { 
      headers,
      mode: 'cors',
    });
    if (!response.ok) throw new Error('Failed to fetch accounts');
    return response.json();
  },

  async getById(id) {
    const response = await fetch(`${API_BASE_URL}/Account/${id}`, { 
      headers,
      mode: 'cors',
    });
    if (!response.ok) throw new Error('Failed to fetch account');
    return response.json();
  },
};

export const transactionsApi = {
  async getAll() {
    const response = await fetch(`${API_BASE_URL}/Transaction`, { 
      headers,
      mode: 'cors',
    });
    if (!response.ok) throw new Error('Failed to fetch transactions');
    return response.json();
  },

  async getById(id) {
    const response = await fetch(`${API_BASE_URL}/Transaction/${id}`, { 
      headers,
      mode: 'cors',
    });
    if (!response.ok) throw new Error('Failed to fetch transaction');
    return response.json();
  },
};

export const akahuApi = {
  async refreshAll() {
    const response = await fetch(`${API_BASE_URL}/Akahu/refresh-all`, {
      method: 'POST',
      headers,
      mode: 'cors',
    });
    if (!response.ok) throw new Error('Failed to refresh data from Akahu');
    return response.json();
  },
};
