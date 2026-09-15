use std::fs;

pub mod object;
pub mod project;

pub const LAVA_FILE_EXT: &'static str = ".lava";

pub struct ProjectMetadata {
    pub file_name: &'static str,
}

impl ProjectMetadata {
    pub fn fetch_file(&self) -> Vec<char> {
        if !self.file_name.ends_with(LAVA_FILE_EXT) {
            panic!("{} is not a .lava file", self.file_name);
        }
        let content = fs::read_to_string(self.file_name).expect("Could not read project file");

        content.chars().collect()
    }
}
