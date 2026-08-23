# Builds Pkmds.Functions as a self-contained executable and packages it for AWS Lambda's
# provided.al2023 (OS-only) base image. AWS does not yet ship a managed .NET 10 Lambda runtime,
# so the app runs as a custom runtime via Amazon.Lambda.AspNetCoreServer.Hosting, which drives
# the Lambda Runtime API loop when it detects it's running inside Lambda.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY Pkmds.Functions/Pkmds.Functions.csproj Pkmds.Functions/
RUN dotnet restore Pkmds.Functions/Pkmds.Functions.csproj -r linux-x64

COPY Pkmds.Functions/ Pkmds.Functions/
RUN dotnet publish Pkmds.Functions/Pkmds.Functions.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    --no-restore \
    -o /app/publish

FROM public.ecr.aws/lambda/provided:al2023
COPY --from=build /app/publish/ ${LAMBDA_TASK_ROOT}
# The Lambda custom-runtime entrypoint must be an executable named "bootstrap".
RUN mv ${LAMBDA_TASK_ROOT}/Pkmds.Functions ${LAMBDA_TASK_ROOT}/bootstrap
CMD [ "bootstrap" ]
