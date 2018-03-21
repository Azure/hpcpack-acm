# HPC Portal

## Angular CLI

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 1.5.3.

### Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The app will automatically reload if you change any of the source files.

### Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

### Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory. Use the `-prod` flag for a production build.

### Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

### Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via [Protractor](http://www.protractortest.org/).

### Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI README](https://github.com/angular/angular-cli/blob/master/README.md).

## Docker on Windows

To develop in Docker on Windows, firstly [register for a Docker account](https://www.docker.com/) and [get Docker](https://www.docker.com/get-docker) on Windows. Then you can use a Docker container as a "runtime box" for your Angular project. Do it as the followings.

Open a "cmd" shell, and log in with your Docker account by

`docker login`

Then cd into the portal project root(like ".../hpc-acm/src/portal") and execute:

`docker run --rm -it -v %cd%:/opt/app -w /opt/app --name portal -p 4200:4200 teracy/angular-cli /bin/bash`

It mounts the current directory `%cd%` to `/opt/app` in the Docker container's system, sets `/opt/app` as the working dirand opens an interactive Bash shell inside the docker container. It also maps the port 4200 of the container to the host's 4200 port.

For the first time(or when you update package.json), you need to install(or update) npm packages. Do it inside the docker container's shell:

`npm install`

Then start the dev server by:

`npm start`

Then you got it!

If you already had `npm install`, then you can start devlopment simply by an one line command from Windows "cmd" shell:

`docker run --rm -v %cd%:/opt/app -w /opt/app --name portal -p 4200:4200 teracy/angular-cli /bin/bash -c "npm start"`

Still, it has to be under the portal project root for `%cd%` to work.

After use, stop the container by

`docker stop portal`

Note that simply "Ctrl+C" doesn't stop a container(which can be observed by `docker container list`).
