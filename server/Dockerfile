FROM node:20-alpine

ENV NODE_ENV=production
WORKDIR /app

COPY package*.json ./
RUN npm ci --omit=dev

COPY relay-server.js security.js ./

EXPOSE 7788
CMD ["node", "relay-server.js"]
