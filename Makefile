# ─── Config ────────────────────────────────────────────────────────────────────
IMAGE_MCP      := mcp-server
DOCKERFILE_MCP := docker/Dockerfile.mcp

DB_PATH_DOCKER := /app/db/family_movies.sqlite

# ─── MCP Server ────────────────────────────────────────────────────────────────

.PHONY: build run shell smoke inspect clean help

## Build the MCP server Docker image
build:
	docker build -f $(DOCKERFILE_MCP) -t $(IMAGE_MCP) .

## Run the stdio MCP server in Docker
## Note: stdio MCP servers need -i, not -d
run:
	@if [ -z "$$(docker images -q $(IMAGE_MCP))" ]; then \
		$(MAKE) build; \
	fi
	docker run --rm -i \
		-e DB_PATH=$(DB_PATH_DOCKER) \
		$(IMAGE_MCP)

## Open a shell inside the MCP container
shell:
	@if [ -z "$$(docker images -q $(IMAGE_MCP))" ]; then \
		$(MAKE) build; \
	fi
	docker run --rm -it \
		-e DB_PATH=$(DB_PATH_DOCKER) \
		$(IMAGE_MCP) \
		/bin/bash

## Verify the MCP server HTTP endpoint responds with the tool list
smoke:
	@if [ -z "$$(docker images -q $(IMAGE_MCP))" ]; then \
		$(MAKE) build; \
	fi
	@CID=$$(docker run -d -e DB_PATH=$(DB_PATH_DOCKER) -p 15000:5000 $(IMAGE_MCP)); \
	sleep 2; \
	RESULT=$$(curl -sf -X POST http://localhost:15000/ \
		-H 'Content-Type: application/json' \
		-H 'Accept: application/json, text/event-stream' \
		-d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' 2>/dev/null); \
	docker stop $$CID >/dev/null; \
	echo "$$RESULT" | python3 -c "\
import sys,json; \
line = next(l for l in sys.stdin if l.startswith('data:')); \
d = json.loads(line[5:]); \
tools = {t['name'] for t in d['result']['tools']}; \
assert 'search_movies' in tools, 'tool missing: '+str(tools); \
print('Smoke test passed — tools:', sorted(tools))"

## Test with MCP Inspector through Docker
inspect:
	@if [ -z "$$(docker images -q $(IMAGE_MCP))" ]; then \
		$(MAKE) build; \
	fi
	npx @modelcontextprotocol/inspector \
		docker run --rm -i \
		-e DB_PATH=$(DB_PATH_DOCKER) \
		$(IMAGE_MCP)

## Remove MCP server image
clean:
	@docker rmi $(IMAGE_MCP) 2>/dev/null || true
	@echo "MCP server image removed."

# ─── Help ──────────────────────────────────────────────────────────────────────

help:
	@echo ""
	@echo "Usage: make <target>"
	@echo ""
	@echo "MCP server:"
	@echo "  build      Build the MCP server Docker image"
	@echo "  run        Run the stdio MCP server in Docker"
	@echo "  shell      Open a shell inside the MCP container"
	@echo "  smoke      Send initialize request and verify server response"
	@echo "  inspect    Test the MCP server with MCP Inspector"
	@echo "  clean      Remove the MCP server image"
	@echo ""
	@echo "Chat app (BotChat copy):"
	@echo "  chat-up       Build and start backend + frontend"
	@echo "  chat-down     Stop the chat services"
	@echo "  chat-build    Build the chat images without starting"
	@echo "  chat-logs     Stream logs for all chat services"
	@echo "  frontend-dev  Run the frontend locally with Vite"
	@echo ""

.DEFAULT_GOAL := help

# ─── Chat App (BotChat copy) ───────────────────────────────────────────────────
DC           := docker compose
COMPOSE_FILE := docker/docker-compose.yml

.PHONY: chat-up chat-down chat-build chat-logs frontend-dev

## Start the chat backend + frontend (builds images first)
chat-up:
	$(DC) -f $(COMPOSE_FILE) up --build -d
	@echo ""
	@echo "  Frontend: http://localhost:5173"
	@echo "  Backend:  http://localhost:8080"
	@echo ""
	@echo "  Set GEMINI_API_KEY in .env before starting."

## Run the frontend locally in Vite development mode
frontend-dev:
	cd frontend && npm install && npm run dev -- --host 0.0.0.0

## Stop the chat services
chat-down:
	$(DC) -f $(COMPOSE_FILE) down

## Build the chat images without starting
chat-build:
	$(DC) -f $(COMPOSE_FILE) build

## Stream logs for all chat services
chat-logs:
	$(DC) -f $(COMPOSE_FILE) logs -f
