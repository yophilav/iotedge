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
pub struct DecryptResponse {
    /// The decrypted form of the data encoded in base 64.
    #[serde(rename = "plaintext")]
    plaintext: String,
}

impl DecryptResponse {
    pub fn new(plaintext: String) -> DecryptResponse {
        DecryptResponse {
            plaintext: plaintext,
        }
    }

    pub fn set_plaintext(&mut self, plaintext: String) {
        self.plaintext = plaintext;
    }

    pub fn with_plaintext(mut self, plaintext: String) -> DecryptResponse {
        self.plaintext = plaintext;
        self
    }

    pub fn plaintext(&self) -> &String {
        &self.plaintext
    }
}
