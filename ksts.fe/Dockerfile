# Accept build-time variable
ARG APP_ENV=development

# Stage 1: Build Angular app
FROM node:20-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci

ARG APP_ENV
COPY . .
RUN npm run build -- --configuration $APP_ENV

# Stage 2: Serve with Nginx
FROM nginx:stable-alpine

# Clean default Nginx static files
RUN rm -rf /usr/share/nginx/html/*

# Copy Angular build output
COPY --from=build /app/dist/ /usr/share/nginx/html

# Optional: custom nginx.conf for SPA support
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
