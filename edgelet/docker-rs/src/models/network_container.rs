/*
 * Docker Engine API
 *
 * The Engine API is an HTTP API served by Docker Engine. It is the API the Docker client uses to communicate with the Engine, so everything the Docker client can do can be done with the API.  Most of the client's commands map directly to API endpoints (e.g. `docker ps` is `GET /containers/json`). The notable exception is running containers, which consists of several API calls.  # Errors  The API uses standard HTTP status codes to indicate the success or failure of the API call. The body of the response will be JSON in the following format:  ``` {   \"message\": \"page not found\" } ```  # Versioning  The API is usually changed in each release of Docker, so API calls are versioned to ensure that clients don't break.  For Docker Engine 17.10, the API version is 1.33. To lock to this version, you prefix the URL with `/v1.33`. For example, calling `/info` is the same as calling `/v1.33/info`.  Engine releases in the near future should support this version of the API, so your client will continue to work even if it is talking to a newer Engine.  In previous versions of Docker, it was possible to access the API without providing a version. This behaviour is now deprecated will be removed in a future version of Docker.  If the API version specified in the URL is not supported by the daemon, a HTTP `400 Bad Request` error message is returned.  The API uses an open schema model, which means server may add extra properties to responses. Likewise, the server will ignore any extra query parameters and request body properties. When you write clients, you need to ignore additional properties in responses to ensure they do not break when talking to newer Docker daemons.  This documentation is for version 1.34 of the API. Use this table to find documentation for previous versions of the API:  Docker version  | API version | Changes ----------------|-------------|--------- 17.10.x | [1.33](https://docs.docker.com/engine/api/v1.33/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-33-api-changes) 17.09.x | [1.32](https://docs.docker.com/engine/api/v1.32/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-32-api-changes) 17.07.x | [1.31](https://docs.docker.com/engine/api/v1.31/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-31-api-changes) 17.06.x | [1.30](https://docs.docker.com/engine/api/v1.30/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-30-api-changes) 17.05.x | [1.29](https://docs.docker.com/engine/api/v1.29/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-29-api-changes) 17.04.x | [1.28](https://docs.docker.com/engine/api/v1.28/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-28-api-changes) 17.03.1 | [1.27](https://docs.docker.com/engine/api/v1.27/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-27-api-changes) 1.13.1 & 17.03.0 | [1.26](https://docs.docker.com/engine/api/v1.26/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-26-api-changes) 1.13.0 | [1.25](https://docs.docker.com/engine/api/v1.25/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-25-api-changes) 1.12.x | [1.24](https://docs.docker.com/engine/api/v1.24/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-24-api-changes) 1.11.x | [1.23](https://docs.docker.com/engine/api/v1.23/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-23-api-changes) 1.10.x | [1.22](https://docs.docker.com/engine/api/v1.22/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-22-api-changes) 1.9.x | [1.21](https://docs.docker.com/engine/api/v1.21/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-21-api-changes) 1.8.x | [1.20](https://docs.docker.com/engine/api/v1.20/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-20-api-changes) 1.7.x | [1.19](https://docs.docker.com/engine/api/v1.19/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-19-api-changes) 1.6.x | [1.18](https://docs.docker.com/engine/api/v1.18/) | [API changes](https://docs.docker.com/engine/api/version-history/#v1-18-api-changes)  # Authentication  Authentication for registries is handled client side. The client has to send authentication details to various endpoints that need to communicate with registries, such as `POST /images/(name)/push`. These are sent as `X-Registry-Auth` header as a Base64 encoded (JSON) string with the following structure:  ``` {   \"username\": \"string\",   \"password\": \"string\",   \"email\": \"string\",   \"serveraddress\": \"string\" } ```  The `serveraddress` is a domain/IP without a protocol. Throughout this structure, double quotes are required.  If you have already got an identity token from the [`/auth` endpoint](#operation/SystemAuth), you can just pass this instead of credentials:  ``` {   \"identitytoken\": \"9cbaf023786cd7...\" } ```
 *
 * OpenAPI spec version: 1.34
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

#[allow(unused_imports)]
use serde_json::Value;

#[derive(Debug, Serialize, Deserialize)]
pub struct NetworkContainer {
    #[serde(rename = "Name")]
    name: Option<String>,
    #[serde(rename = "EndpointID")]
    endpoint_id: Option<String>,
    #[serde(rename = "MacAddress")]
    mac_address: Option<String>,
    #[serde(rename = "IPv4Address")]
    i_pv4_address: Option<String>,
    #[serde(rename = "IPv6Address")]
    i_pv6_address: Option<String>,
}

impl NetworkContainer {
    pub fn new() -> NetworkContainer {
        NetworkContainer {
            name: None,
            endpoint_id: None,
            mac_address: None,
            i_pv4_address: None,
            i_pv6_address: None,
        }
    }

    pub fn set_name(&mut self, name: String) {
        self.name = Some(name);
    }

    pub fn with_name(mut self, name: String) -> NetworkContainer {
        self.name = Some(name);
        self
    }

    pub fn name(&self) -> Option<&String> {
        self.name.as_ref()
    }

    pub fn reset_name(&mut self) {
        self.name = None;
    }

    pub fn set_endpoint_id(&mut self, endpoint_id: String) {
        self.endpoint_id = Some(endpoint_id);
    }

    pub fn with_endpoint_id(mut self, endpoint_id: String) -> NetworkContainer {
        self.endpoint_id = Some(endpoint_id);
        self
    }

    pub fn endpoint_id(&self) -> Option<&String> {
        self.endpoint_id.as_ref()
    }

    pub fn reset_endpoint_id(&mut self) {
        self.endpoint_id = None;
    }

    pub fn set_mac_address(&mut self, mac_address: String) {
        self.mac_address = Some(mac_address);
    }

    pub fn with_mac_address(mut self, mac_address: String) -> NetworkContainer {
        self.mac_address = Some(mac_address);
        self
    }

    pub fn mac_address(&self) -> Option<&String> {
        self.mac_address.as_ref()
    }

    pub fn reset_mac_address(&mut self) {
        self.mac_address = None;
    }

    pub fn set_i_pv4_address(&mut self, i_pv4_address: String) {
        self.i_pv4_address = Some(i_pv4_address);
    }

    pub fn with_i_pv4_address(mut self, i_pv4_address: String) -> NetworkContainer {
        self.i_pv4_address = Some(i_pv4_address);
        self
    }

    pub fn i_pv4_address(&self) -> Option<&String> {
        self.i_pv4_address.as_ref()
    }

    pub fn reset_i_pv4_address(&mut self) {
        self.i_pv4_address = None;
    }

    pub fn set_i_pv6_address(&mut self, i_pv6_address: String) {
        self.i_pv6_address = Some(i_pv6_address);
    }

    pub fn with_i_pv6_address(mut self, i_pv6_address: String) -> NetworkContainer {
        self.i_pv6_address = Some(i_pv6_address);
        self
    }

    pub fn i_pv6_address(&self) -> Option<&String> {
        self.i_pv6_address.as_ref()
    }

    pub fn reset_i_pv6_address(&mut self) {
        self.i_pv6_address = None;
    }
}
