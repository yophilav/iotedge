/*
 * IotHub Gateway Service APIs
 *
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 * OpenAPI spec version: Service
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

#[allow(unused_imports)]
use serde_json::Value;

#[derive(Debug, Serialize, Deserialize)]
pub struct Device {
    #[serde(rename = "deviceId", skip_serializing_if = "Option::is_none")]
    device_id: Option<String>,
    #[serde(rename = "generationId", skip_serializing_if = "Option::is_none")]
    generation_id: Option<String>,
    #[serde(rename = "etag", skip_serializing_if = "Option::is_none")]
    etag: Option<String>,
    #[serde(rename = "connectionState", skip_serializing_if = "Option::is_none")]
    connection_state: Option<String>,
    #[serde(rename = "status", skip_serializing_if = "Option::is_none")]
    status: Option<String>,
    #[serde(rename = "statusReason", skip_serializing_if = "Option::is_none")]
    status_reason: Option<String>,
    #[serde(rename = "connectionStateUpdatedTime", skip_serializing_if = "Option::is_none")]
    connection_state_updated_time: Option<String>,
    #[serde(rename = "statusUpdatedTime", skip_serializing_if = "Option::is_none")]
    status_updated_time: Option<String>,
    #[serde(rename = "lastActivityTime", skip_serializing_if = "Option::is_none")]
    last_activity_time: Option<String>,
    #[serde(rename = "cloudToDeviceMessageCount", skip_serializing_if = "Option::is_none")]
    cloud_to_device_message_count: Option<i32>,
    #[serde(rename = "authentication", skip_serializing_if = "Option::is_none")]
    authentication: Option<::models::AuthenticationMechanism>,
    /// Capabilities get saved in DMC but rendered out on this object  Based on API Version we set this to null so we dont render them.
    #[serde(rename = "capabilities", skip_serializing_if = "Option::is_none")]
    capabilities: Option<::models::DeviceCapabilities>,
}

impl Device {
    pub fn new() -> Device {
        Device {
            device_id: None,
            generation_id: None,
            etag: None,
            connection_state: None,
            status: None,
            status_reason: None,
            connection_state_updated_time: None,
            status_updated_time: None,
            last_activity_time: None,
            cloud_to_device_message_count: None,
            authentication: None,
            capabilities: None,
        }
    }

    pub fn set_device_id(&mut self, device_id: String) {
        self.device_id = Some(device_id);
    }

    pub fn with_device_id(mut self, device_id: String) -> Device {
        self.device_id = Some(device_id);
        self
    }

    pub fn device_id(&self) -> Option<&String> {
        self.device_id.as_ref()
    }

    pub fn reset_device_id(&mut self) {
        self.device_id = None;
    }

    pub fn set_generation_id(&mut self, generation_id: String) {
        self.generation_id = Some(generation_id);
    }

    pub fn with_generation_id(mut self, generation_id: String) -> Device {
        self.generation_id = Some(generation_id);
        self
    }

    pub fn generation_id(&self) -> Option<&String> {
        self.generation_id.as_ref()
    }

    pub fn reset_generation_id(&mut self) {
        self.generation_id = None;
    }

    pub fn set_etag(&mut self, etag: String) {
        self.etag = Some(etag);
    }

    pub fn with_etag(mut self, etag: String) -> Device {
        self.etag = Some(etag);
        self
    }

    pub fn etag(&self) -> Option<&String> {
        self.etag.as_ref()
    }

    pub fn reset_etag(&mut self) {
        self.etag = None;
    }

    pub fn set_connection_state(&mut self, connection_state: String) {
        self.connection_state = Some(connection_state);
    }

    pub fn with_connection_state(mut self, connection_state: String) -> Device {
        self.connection_state = Some(connection_state);
        self
    }

    pub fn connection_state(&self) -> Option<&String> {
        self.connection_state.as_ref()
    }

    pub fn reset_connection_state(&mut self) {
        self.connection_state = None;
    }

    pub fn set_status(&mut self, status: String) {
        self.status = Some(status);
    }

    pub fn with_status(mut self, status: String) -> Device {
        self.status = Some(status);
        self
    }

    pub fn status(&self) -> Option<&String> {
        self.status.as_ref()
    }

    pub fn reset_status(&mut self) {
        self.status = None;
    }

    pub fn set_status_reason(&mut self, status_reason: String) {
        self.status_reason = Some(status_reason);
    }

    pub fn with_status_reason(mut self, status_reason: String) -> Device {
        self.status_reason = Some(status_reason);
        self
    }

    pub fn status_reason(&self) -> Option<&String> {
        self.status_reason.as_ref()
    }

    pub fn reset_status_reason(&mut self) {
        self.status_reason = None;
    }

    pub fn set_connection_state_updated_time(&mut self, connection_state_updated_time: String) {
        self.connection_state_updated_time = Some(connection_state_updated_time);
    }

    pub fn with_connection_state_updated_time(
        mut self,
        connection_state_updated_time: String,
    ) -> Device {
        self.connection_state_updated_time = Some(connection_state_updated_time);
        self
    }

    pub fn connection_state_updated_time(&self) -> Option<&String> {
        self.connection_state_updated_time.as_ref()
    }

    pub fn reset_connection_state_updated_time(&mut self) {
        self.connection_state_updated_time = None;
    }

    pub fn set_status_updated_time(&mut self, status_updated_time: String) {
        self.status_updated_time = Some(status_updated_time);
    }

    pub fn with_status_updated_time(mut self, status_updated_time: String) -> Device {
        self.status_updated_time = Some(status_updated_time);
        self
    }

    pub fn status_updated_time(&self) -> Option<&String> {
        self.status_updated_time.as_ref()
    }

    pub fn reset_status_updated_time(&mut self) {
        self.status_updated_time = None;
    }

    pub fn set_last_activity_time(&mut self, last_activity_time: String) {
        self.last_activity_time = Some(last_activity_time);
    }

    pub fn with_last_activity_time(mut self, last_activity_time: String) -> Device {
        self.last_activity_time = Some(last_activity_time);
        self
    }

    pub fn last_activity_time(&self) -> Option<&String> {
        self.last_activity_time.as_ref()
    }

    pub fn reset_last_activity_time(&mut self) {
        self.last_activity_time = None;
    }

    pub fn set_cloud_to_device_message_count(&mut self, cloud_to_device_message_count: i32) {
        self.cloud_to_device_message_count = Some(cloud_to_device_message_count);
    }

    pub fn with_cloud_to_device_message_count(
        mut self,
        cloud_to_device_message_count: i32,
    ) -> Device {
        self.cloud_to_device_message_count = Some(cloud_to_device_message_count);
        self
    }

    pub fn cloud_to_device_message_count(&self) -> Option<&i32> {
        self.cloud_to_device_message_count.as_ref()
    }

    pub fn reset_cloud_to_device_message_count(&mut self) {
        self.cloud_to_device_message_count = None;
    }

    pub fn set_authentication(&mut self, authentication: ::models::AuthenticationMechanism) {
        self.authentication = Some(authentication);
    }

    pub fn with_authentication(
        mut self,
        authentication: ::models::AuthenticationMechanism,
    ) -> Device {
        self.authentication = Some(authentication);
        self
    }

    pub fn authentication(&self) -> Option<&::models::AuthenticationMechanism> {
        self.authentication.as_ref()
    }

    pub fn reset_authentication(&mut self) {
        self.authentication = None;
    }

    pub fn set_capabilities(&mut self, capabilities: ::models::DeviceCapabilities) {
        self.capabilities = Some(capabilities);
    }

    pub fn with_capabilities(mut self, capabilities: ::models::DeviceCapabilities) -> Device {
        self.capabilities = Some(capabilities);
        self
    }

    pub fn capabilities(&self) -> Option<&::models::DeviceCapabilities> {
        self.capabilities.as_ref()
    }

    pub fn reset_capabilities(&mut self) {
        self.capabilities = None;
    }
}
