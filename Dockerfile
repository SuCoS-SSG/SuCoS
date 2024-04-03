ARG BASE_IMAGE

FROM ${BASE_IMAGE}

# Copy the published output from the build stage
COPY ./* /usr/local/bin/

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
