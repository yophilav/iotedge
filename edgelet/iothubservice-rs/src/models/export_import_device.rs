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
pub struct ExportImportDevice {
    #[serde(rename = "id", skip_serializing_if = "Option::is_none")]
    id: Option<String>,
    #[serde(rename = "moduleId", skip_serializing_if = "Option::is_none")]
    module_id: Option<String>,
    #[serde(rename = "eTag", skip_serializing_if = "Option::is_none")]
    e_tag: Option<String>,
    #[serde(rename = "importMode", skip_serializing_if = "Option::is_none")]
    import_mode: Option<String>,
    #[serde(rename = "status", skip_serializing_if = "Option::is_none")]
    status: Option<String>,
    #[serde(rename = "statusReason", skip_serializing_if = "Option::is_none")]
    status_reason: Option<String>,
    #[serde(rename = "authentication", skip_serializing_if = "Option::is_none")]
    authentication: Option<::models::AuthenticationMechanism>,
    #[serde(rename = "twinETag", skip_serializing_if = "Option::is_none")]
    twin_e_tag: Option<String>,
    #[serde(rename = "tags", skip_serializing_if = "Option::is_none")]
    tags: Option<::std::collections::HashMap<String, Value>>,
    #[serde(rename = "properties", skip_serializing_if = "Option::is_none")]
    properties: Option<::models::PropertyContainer>,
    #[serde(rename = "capabilities", skip_serializing_if = "Option::is_none")]
    capabilities: Option<::models::DeviceCapabilities>,
    #[serde(rename = "managedBy", skip_serializing_if = "Option::is_none")]
    managed_by: Option<String>,
}

impl ExportImportDevice {
    pub fn new() -> ExportImportDevice {
        ExportImportDevice {
            id: None,
            module_id: None,
            e_tag: None,
            import_mode: None,
            status: None,
            status_reason: None,
            authentication: None,
            twin_e_tag: None,
            tags: None,
            properties: None,
            capabilities: None,
            managed_by: None,
        }
    }

    pub fn set_id(&mut self, id: String) {
        self.id = Some(id);
    }

    pub fn with_id(mut self, id: String) -> ExportImportDevice {
        self.id = Some(id);
        self
    }

    pub fn id(&self) -> Option<&String> {
        self.id.as_ref()
    }

    pub fn reset_id(&mut self) {
        self.id = None;
    }

    pub fn set_module_id(&mut self, module_id: String) {
        self.module_id = Some(module_id);
    }

    pub fn with_module_id(mut self, module_id: String) -> ExportImportDevice {
        self.module_id = Some(module_id);
        self
    }

    pub fn module_id(&self) -> Option<&String> {
        self.module_id.as_ref()
    }

    pub fn reset_module_id(&mut self) {
        self.module_id = None;
    }

    pub fn set_e_tag(&mut self, e_tag: String) {
        self.e_tag = Some(e_tag);
    }

    pub fn with_e_tag(mut self, e_tag: String) -> ExportImportDevice {
        self.e_tag = Some(e_tag);
        self
    }

    pub fn e_tag(&self) -> Option<&String> {
        self.e_tag.as_ref()
    }

    pub fn reset_e_tag(&mut self) {
        self.e_tag = None;
    }

    pub fn set_import_mode(&mut self, import_mode: String) {
        self.import_mode = Some(import_mode);
    }

    pub fn with_import_mode(mut self, import_mode: String) -> ExportImportDevice {
        self.import_mode = Some(import_mode);
        self
    }

    pub fn import_mode(&self) -> Option<&String> {
        self.import_mode.as_ref()
    }

    pub fn reset_import_mode(&mut self) {
        self.import_mode = None;
    }

    pub fn set_status(&mut self, status: String) {
        self.status = Some(status);
    }

    pub fn with_status(mut self, status: String) -> ExportImportDevice {
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

    pub fn with_status_reason(mut self, status_reason: String) -> ExportImportDevice {
        self.status_reason = Some(status_reason);
        self
    }

    pub fn status_reason(&self) -> Option<&String> {
        self.status_reason.as_ref()
    }

    pub fn reset_status_reason(&mut self) {
        self.status_reason = None;
    }

    pub fn set_authentication(&mut self, authentication: ::models::AuthenticationMechanism) {
        self.authentication = Some(authentication);
    }

    pub fn with_authentication(
        mut self,
        authentication: ::models::AuthenticationMechanism,
    ) -> ExportImportDevice {
        self.authentication = Some(authentication);
        self
    }

    pub fn authentication(&self) -> Option<&::models::AuthenticationMechanism> {
        self.authentication.as_ref()
    }

    pub fn reset_authentication(&mut self) {
        self.authentication = None;
    }

    pub fn set_twin_e_tag(&mut self, twin_e_tag: String) {
        self.twin_e_tag = Some(twin_e_tag);
    }

    pub fn with_twin_e_tag(mut self, twin_e_tag: String) -> ExportImportDevice {
        self.twin_e_tag = Some(twin_e_tag);
        self
    }

    pub fn twin_e_tag(&self) -> Option<&String> {
        self.twin_e_tag.as_ref()
    }

    pub fn reset_twin_e_tag(&mut self) {
        self.twin_e_tag = None;
    }

    pub fn set_tags(&mut self, tags: ::std::collections::HashMap<String, Value>) {
        self.tags = Some(tags);
    }

    pub fn with_tags(
        mut self,
        tags: ::std::collections::HashMap<String, Value>,
    ) -> ExportImportDevice {
        self.tags = Some(tags);
        self
    }

    pub fn tags(&self) -> Option<&::std::collections::HashMap<String, Value>> {
        self.tags.as_ref()
    }

    pub fn reset_tags(&mut self) {
        self.tags = None;
    }

    pub fn set_properties(&mut self, properties: ::models::PropertyContainer) {
        self.properties = Some(properties);
    }

    pub fn with_properties(
        mut self,
        properties: ::models::PropertyContainer,
    ) -> ExportImportDevice {
        self.properties = Some(properties);
        self
    }

    pub fn properties(&self) -> Option<&::models::PropertyContainer> {
        self.properties.as_ref()
    }

    pub fn reset_properties(&mut self) {
        self.properties = None;
    }

    pub fn set_capabilities(&mut self, capabilities: ::models::DeviceCapabilities) {
        self.capabilities = Some(capabilities);
    }

    pub fn with_capabilities(
        mut self,
        capabilities: ::models::DeviceCapabilities,
    ) -> ExportImportDevice {
        self.capabilities = Some(capabilities);
        self
    }

    pub fn capabilities(&self) -> Option<&::models::DeviceCapabilities> {
        self.capabilities.as_ref()
    }

    pub fn reset_capabilities(&mut self) {
        self.capabilities = None;
    }

    pub fn set_managed_by(&mut self, managed_by: String) {
        self.managed_by = Some(managed_by);
    }

    pub fn with_managed_by(mut self, managed_by: String) -> ExportImportDevice {
        self.managed_by = Some(managed_by);
        self
    }

    pub fn managed_by(&self) -> Option<&String> {
        self.managed_by.as_ref()
    }

    pub fn reset_managed_by(&mut self) {
        self.managed_by = None;
    }
}
