# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY frontend/TransactionDisputePortal.Web/package*.json ./

# Install dependencies
# Use npm install instead of npm ci to avoid lockfile-related failures in build environment
RUN npm install --no-audit --no-fund

# Copy source code
COPY frontend/TransactionDisputePortal.Web/ .

# Build application
RUN npm run build

# Runtime stage
FROM node:20-alpine AS runtime
WORKDIR /app

# Install serve to run the static site
RUN npm install -g serve

# Copy built application from build stage
COPY --from=build /app/dist ./dist

# Expose port
EXPOSE 5173

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
	CMD wget --no-verbose --tries=1 --spider http://localhost:5173/ || exit 1

# Serve the application
CMD ["serve", "-s", "dist", "-l", "5173"]
