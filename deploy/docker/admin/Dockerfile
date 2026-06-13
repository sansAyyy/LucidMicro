# syntax=docker/dockerfile:1

FROM node:24-alpine AS build

WORKDIR /src/frontend

ARG VITE_API_BASE_URL=http://localhost:49953
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml frontend/tsconfig.base.json ./
COPY frontend/apps ./apps

RUN corepack enable
RUN pnpm install --frozen-lockfile
RUN pnpm --filter @lucid-micro/admin build

FROM nginx:1.29-alpine AS runtime

COPY deploy/docker/admin/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/frontend/apps/admin/dist /usr/share/nginx/html

EXPOSE 80
