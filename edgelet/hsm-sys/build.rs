// Copyright (c) Microsoft. All rights reserved.
extern crate cmake;

use std::env;
use std::path::Path;
use std::process::Command;

use cmake::Config;

#[cfg(windows)]
const SSL_OPTION: &str = "use_schannel";

#[cfg(unix)]
const SSL_OPTION: &str = "use_openssl";

trait SetPlatformDefines {
    fn set_platform_defines(&mut self) -> &mut Self;
    fn set_build_shared(&mut self) -> &mut Self;
}

impl SetPlatformDefines for Config {
    #[cfg(windows)]
    fn set_platform_defines(&mut self) -> &mut Self {
        // C-shared library wants Windows flags (/DWIN32 /D_WINDOWS) for Windows,
        // and the cmake library overrides this.
        self.cflag("/DWIN32")
            .cxxflag("/DWIN32")
            .cflag("/D_WINDOWS")
            .cxxflag("/D_WINDOWS")
    }

    #[cfg(unix)]
    fn set_platform_defines(&mut self) -> &mut Self {
        let rv = if env::var("PROFILE").unwrap().to_lowercase() == "release"
            || !env::var("TARGET").unwrap().starts_with("x86_64")
            || env::var("NO_VALGRIND").is_ok()
        {
            "OFF"
        } else {
            "ON"
        };
        //CMAKE_SYSROOT
        if let Ok(sysroot) = env::var("SYSROOT") {
            self.define("run_valgrind", rv)
                .define("CMAKE_SYSROOT", sysroot)
        } else {
            self.define("run_valgrind", rv)
        }
    }

    // The "debug_assertions" configuration flag seems to be the way to detect
    // if this is a "dev" build or any other kind of build.
    #[cfg(debug_assertions)]
    fn set_build_shared(&mut self) -> &mut Self {
        self.define("BUILD_SHARED", "OFF")
    }

    #[cfg(not(debug_assertions))]
    fn set_build_shared(&mut self) -> &mut Self {
        self.define("BUILD_SHARED", "ON")
    }
}

fn main() {
    // Clone Azure C -shared library
    let c_shared_repo = "azure-iot-hsm-c/azure-c-shared-utility";

    println!("#Start Update C-Shared Utilities");
    if !Path::new(&format!("{}/.git", c_shared_repo)).exists() {
        let _ = Command::new("git")
            .arg("submodule")
            .arg("update")
            .arg("--init")
            .arg("--recursive")
            .status()
            .expect("submodule update failed");
    }

    println!("#Done Updating C-Shared Utilities");

    // make the C libary at azure-iot-hsm-c (currently a subdirectory in this
    // crate)
    // Always make the Release version because Rust links to the Release CRT.
    // (This is especially important for Windows)
    println!("#Start building HSM dev-mode library");
    let iothsm = Config::new("azure-iot-hsm-c")
        .define(SSL_OPTION, "ON")
        .define("CMAKE_BUILD_TYPE", "Release")
        .define("run_unittests", "ON")
        .define("use_default_uuid", "ON")
        .set_platform_defines()
        .set_build_shared()
        .profile("Release")
        .build();

    println!("#Done building HSM dev-mode library");

    // where to find the library (The "link-lib" should match the library name
    // defined in the CMakefile.txt)

    println!("cargo:rerun-if-env-changed=NO_VALGRIND");
    // For libraries which will just install in target directory
    println!("cargo:rustc-link-search=native={}", iothsm.display());
    // For libraries (ie. C Shared) which will install in $target/lib
    println!("cargo:rustc-link-search=native={}/lib", iothsm.display());
    println!("cargo:rustc-link-lib=iothsm");

    // we need to explicitly link with c shared util only when we build the C
    // library as a static lib which we do only in rust debug builds
    #[cfg(debug_assertions)]
    println!("cargo:rustc-link-lib=aziotsharedutil");
}
