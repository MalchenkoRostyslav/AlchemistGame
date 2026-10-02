export const API = 'https://alchemistgame.onrender.com/api';

async function request(path, { method = 'GET', body } = {}) {
  let res;

  // Формуємо заголовки
  const headers = {};
  if (body) {
    headers['Content-Type'] = 'application/json';
  }

  // Автоматично додаємо JWT-токен до кожного запиту, якщо він є у localStorage
  const token = localStorage.getItem('token');
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  try {
    res = await fetch(API + path, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined
    });
  } catch (err) {
    throw new Error(`Немає зв'язку з сервером (${API}). Перевірте інтернет-з'єднання або зачекайте, поки Render вийде зі сплячого режиму.`);
  }

  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }

  if (!res.ok) {
    // Якщо токен недійсний або протермінований (401 Unauthorized)
    if (res.status === 401) {
      localStorage.removeItem('token');
      // За потреби тут можна додати window.location.reload(); щоб викинути на екран входу
    }
    const msg = (data && (data.message || data.title)) || (typeof data === 'string' && data) || `Помилка ${res.status}`;
    throw new Error(msg);
  }

  return data;
}

export const api = {
  get: (p) => request(p),
  post: (p, body) => request(p, { method: 'POST', body }),
  del: (p) => request(p, { method: 'DELETE' })
};