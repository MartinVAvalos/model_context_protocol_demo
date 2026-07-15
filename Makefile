# ─── Config ────────────────────────────────────────────────────────────────────
IMAGE_MCP      := mcp-server
DOCKERFILE_MCP := docker/Dockerfile.mcp

DB_PATH_LOCAL  := db/sales.sqlite
DB_PATH_DOCKER := /app/db/sales.sqlite
PYTHON_LOCAL   ?= python3

# ─── MCP Server ────────────────────────────────────────────────────────────────

.PHONY: build run shell test smoke inspect clean help

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
		/bin/sh

## Run Python smoke test inside Docker
smoke:
	@if [ -z "$$(docker images -q $(IMAGE_MCP))" ]; then \
		$(MAKE) build; \
	fi
	docker run --rm \
		-e DB_PATH=$(DB_PATH_DOCKER) \
		$(IMAGE_MCP) \
		python -c "import json; from src.server import _handle_get_orders_by_month; r = _handle_get_orders_by_month({'year': 2025, 'month': 1}); p = json.loads(r[0].text); assert p['count'] == 4; print(json.dumps(p, indent=2))"

## Run local Python smoke test
test:
	@if ! $(PYTHON_LOCAL) -c "import mcp" >/dev/null 2>&1; then \
		echo "Local dependency 'mcp' not found. Running Docker smoke test instead."; \
		$(MAKE) smoke; \
	else \
		DB_PATH=$(DB_PATH_LOCAL) $(PYTHON_LOCAL) -c "import json; from src.server import _handle_get_orders_by_month; r = _handle_get_orders_by_month({'year': 2025, 'month': 1}); p = json.loads(r[0].text); assert p['count'] == 4; print('Local Python test passed. January 2025 order count:', p['count'])"; \
	fi

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
	@echo "  build    Build the MCP server Docker image"
	@echo "  run      Run the stdio MCP server in Docker"
	@echo "  shell    Open a shell inside the MCP container"
	@echo "  smoke    Run Python smoke test inside Docker"
	@echo "  test     Run local Python smoke test"
	@echo "  inspect  Test the MCP server with MCP Inspector"
	@echo "  clean    Remove the MCP server image"
	@echo ""

.DEFAULT_GOAL := help