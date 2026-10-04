# Imagen del servicio Steam con Node y directorio de datos persistentes.

FROM node:24-alpine
WORKDIR /app
COPY --chown=node:node package.json ./
COPY --chown=node:node src ./src
USER node
ENV PORT=34871 HOST=0.0.0.0
EXPOSE 34871
CMD ["node", "src/index.mjs"]
