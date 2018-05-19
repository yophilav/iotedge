// Copyright (c) Microsoft. All rights reserved.

use std::fmt;

use failure::Fail;
use futures::Future;

#[derive(Clone, Debug, Deserialize, PartialEq, Serialize)]
pub enum AuthType {
    None,
    Sas,
    X509,
}

impl fmt::Display for AuthType {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        let s = match *self {
            AuthType::None => "None",
            AuthType::Sas => "Sas",
            AuthType::X509 => "X509",
        };
        write!(f, "{}", s)
    }
}

pub trait Identity {
    fn module_id(&self) -> &str;
    fn managed_by(&self) -> &str;
    fn generation_id(&self) -> &str;
    fn auth_type(&self) -> AuthType;
}

pub struct IdentitySpec {
    module_id: String,
    generation_id: Option<String>,
}

impl IdentitySpec {
    pub fn new(module_id: &str) -> IdentitySpec {
        IdentitySpec {
            module_id: module_id.to_string(),
            generation_id: None,
        }
    }

    pub fn module_id(&self) -> &str {
        &self.module_id
    }

    pub fn generation_id(&self) -> Option<&String> {
        self.generation_id.as_ref()
    }

    pub fn with_generation_id(mut self, generation_id: String) -> Self {
        self.generation_id = Some(generation_id);
        self
    }
}

pub trait IdentityManager {
    type Identity: Identity;
    type Error: Fail;
    type CreateFuture: Future<Item = Self::Identity, Error = Self::Error>;
    type UpdateFuture: Future<Item = Self::Identity, Error = Self::Error>;
    type ListFuture: Future<Item = Vec<Self::Identity>, Error = Self::Error>;
    type GetFuture: Future<Item = Option<Self::Identity>, Error = Self::Error>;
    type DeleteFuture: Future<Item = (), Error = Self::Error>;

    fn create(&mut self, id: IdentitySpec) -> Self::CreateFuture;
    fn update(&mut self, id: IdentitySpec) -> Self::UpdateFuture;
    fn list(&self) -> Self::ListFuture;
    fn get(&self, id: IdentitySpec) -> Self::GetFuture;
    fn delete(&mut self, id: IdentitySpec) -> Self::DeleteFuture;
}
