use std::env;

pub enum Platform {
    Windows,
    MacOS,
    Linux,
}

pub fn get_platform() -> Platform {
    match env::consts::OS {
        "macos" => Platform::MacOS,
        &_ => panic!("Unsupported build platform"),
    }
}
