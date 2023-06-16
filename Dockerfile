ARG BASE_IMAGE

FROM ${BASE_IMAGE}

# Set the working directory in the container
WORKDIR /app

# Copy the published output from the build stage
COPY ./* .

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

RUN chmod +x ./SuCoS
RUN ln -s /app/SuCoS /usr/local/bin/SuCoS
