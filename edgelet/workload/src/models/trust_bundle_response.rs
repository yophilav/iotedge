/*
 * IoT Edge Module Workload API
 *
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 * OpenAPI spec version: 2018-06-28
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

#[allow(unused_imports)]
use serde_json::Value;

#[derive(Debug, Serialize, Deserialize)]
pub struct TrustBundleResponse {
    /// Base64 encoded PEM formatted byte array containing the trusted certificates.
    #[serde(rename = "certificate")]
    certificate: String,
}

impl TrustBundleResponse {
    pub fn new(certificate: String) -> TrustBundleResponse {
        TrustBundleResponse { certificate }
    }

    pub fn set_certificate(&mut self, certificate: String) {
        self.certificate = certificate;
    }

    pub fn with_certificate(mut self, certificate: String) -> TrustBundleResponse {
        self.certificate = certificate;
        self
    }

    pub fn certificate(&self) -> &String {
        &self.certificate
    }
}
