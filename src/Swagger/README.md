## Swagger Spec

The Swagger spec is at https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md

## Generating Code
The code generator is at https://github.com/swagger-api/swagger-codegen, version 2.3.1. It's a Java Jar file, requiring Java 7 or higher.

Download it at http://central.maven.org/maven2/io/swagger/swagger-codegen-cli/2.3.1/swagger-codegen-cli-2.3.1.jar

For help:

```
java -jar swagger-codegen-cli.jar help
```

For a list of avaialbe languages:

```
java -jar swagger-codegen-cli.jar langs
```

For configuration file of a language, say csharp:

```
java -jar swagger-codegen-cli.jar config-help -l csharp
```

To generate csharp client with an config file:

```
java -jar swagger-codegen-cli.jar generate -l csharp -c config\csharp.json -i swagger.yaml -o generated\csharp
```
