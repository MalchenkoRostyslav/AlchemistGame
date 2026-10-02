// Єдине місце, де вказана адреса API.
// Локально (localhost) використовується локальний бекенд, на хостингу: адреса Render.
const API_BASE = (location.hostname === 'localhost' || location.hostname === '127.0.0.1')
    ? 'http://localhost:5012/api'
    : 'https://ВАШ-СЕРВІС.onrender.com/api'; // <-- ЗАМІНІТЬ на адресу з Render
