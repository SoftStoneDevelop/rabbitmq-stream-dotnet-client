# vim: noexpandtab:ts=4:sw=4
.PHONY: build test format rabbitmq-server stop-rabbitmq-server run-test-in-docker publish-github-pages

RABBITMQ_DOCKER_NAME ?= rabbitmq-stream-dotnet-client-rabbitmq

build:
	dotnet build $(CURDIR)/Build.csproj

test: build
	dotnet test -c Debug $(CURDIR)/Tests/Tests.csproj --no-build --logger:"console;verbosity=detailed" /p:AltCover=true

format:
	dotnet format $(CURDIR)/rabbitmq-stream-dotnet-client.sln

rabbitmq-server:
	$(CURDIR)/.ci/ubuntu/gha-setup.sh

stop-rabbitmq-server:
	docker stop $(RABBITMQ_DOCKER_NAME)

## Simulate the CI tests
## in GitHub Action the number of the CPU is limited
run-test-in-docker:
	docker stop dotnet-test || true
	docker build -t stream-dotnet-test -f Docker/Dockerfile  . && \
	docker run -d --rm --name dotnet-test -v $(shell pwd):/source --cpuset-cpus="1" stream-dotnet-test && \
	docker exec -it dotnet-test /bin/sh -c "cd /source && make test" || true

## publish the documentation on github pages
## you should execute this command only on the `main` branch
publish-github-pages:
	## Create the PDF
	docker run  -it -v $(shell pwd)/docs/:/client_doc/  asciidoctor/docker-asciidoctor /bin/bash -c "cd /client_doc/asciidoc &&  asciidoctor-pdf index.adoc"
	## Create the HTML
	docker run  -it -v $(shell pwd)/docs/:/client_doc/  asciidoctor/docker-asciidoctor /bin/bash -c "cd /client_doc/asciidoc &&  asciidoctor index.adoc"
	## copy the PDF and HTML to temp folder
	rm -rf docs/temp
	mkdir -p docs/temp
	cp docs/asciidoc/index.pdf docs/temp/dotnet-stream-client.pdf
	cp docs/asciidoc/index.html docs/temp/index.html
	## check out the gh-pages branch
	git checkout gh-pages
	## copy the PDF and HTML to the root folder
	mv docs/temp/dotnet-stream-client.pdf stable/dotnet-stream-client.pdf
	mv docs/temp/index.html stable/htmlsingle/index.html
	## commit and push
	git add stable/dotnet-stream-client.pdf
	git add stable/htmlsingle/index.html
	git commit -m "Update the documentation"
	git push origin gh-pages
	## go back to the main branch
	git checkout main
