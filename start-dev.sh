#!/bin/bash

# Capitec Transaction Dispute Portal - Local Development Startup Script

echo "================================================"
echo "Capitec Transaction Dispute Portal - Local Dev Start"
echo "================================================"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
	echo "❌ .NET SDK is not installed. Please install .NET 10 SDK first."
	exit 1
fi

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
	echo "❌ Node.js is not installed. Please install Node.js 18+ first."
	exit 1
fi

echo -e "${GREEN}✓ Prerequisites validated${NC}"

# Start Backend
echo -e "${BLUE}Starting Backend API...${NC}"
cd backend/TransactionDisputePortal.Api
dotnet run &
BACKEND_PID=$!
echo -e "${GREEN}Backend started (PID: $BACKEND_PID)${NC}"
sleep 5

# Start Frontend
echo -e "${BLUE}Starting Frontend...${NC}"
cd ../../frontend/TransactionDisputePortal.Web
npm install > /dev/null 2>&1
npm run dev &
FRONTEND_PID=$!
echo -e "${GREEN}Frontend started (PID: $FRONTEND_PID)${NC}"

echo ""
echo "================================================"
echo -e "${GREEN}Application Started!${NC}"
echo "================================================"
echo -e "Frontend:  ${BLUE}http://localhost:5173${NC}"
echo -e "Backend:   ${BLUE}http://localhost:5115${NC}"
echo -e "API Docs:  ${BLUE}http://localhost:5115/openapi/v1.json${NC}"
echo ""
echo "Press Ctrl+C to stop both services"
echo "================================================"

# Keep script running
wait $BACKEND_PID $FRONTEND_PID
