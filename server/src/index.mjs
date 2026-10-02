import { createService } from './service.mjs';
const port = Number(process.env.PORT || 34871);
if (!Number.isInteger(port) || port < 1 || port > 65535) throw new Error('PORT no válido.');
const server = createService({ apiKey: process.env.STEAM_API_KEY, publicUrl: process.env.PUBLIC_URL,
  operator: process.env.OPERATOR_NAME, country: process.env.HOSTING_COUNTRY, contact: process.env.PRIVACY_CONTACT });
server.listen(port, process.env.HOST || '127.0.0.1', () => {
  console.log(`Checkpoint Steam service en puerto ${port}. Clave ${process.env.STEAM_API_KEY ? 'configurada' : 'sin configurar'}.`);
});
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => server.close(() => process.exit(0)));
