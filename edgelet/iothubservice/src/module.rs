// Copyright (c) Microsoft. All rights reserved.

use hyper::client::Connect;

use client::Client;

pub struct ModuleClient<C: Connect> {
    _client: Client<C>,
    device_id: String,
    module_id: String,
}

impl<C: Connect> ModuleClient<C> {
    pub fn new(client: Client<C>, device_id: &str, module_id: &str) -> ModuleClient<C> {
        ModuleClient {
            _client: client,
            device_id: device_id.to_string(),
            module_id: module_id.to_string(),
        }
    }

    pub fn device_id(&self) -> &str {
        self.device_id.as_ref()
    }

    pub fn module_id(&self) -> &str {
        self.module_id.as_ref()
    }
}
