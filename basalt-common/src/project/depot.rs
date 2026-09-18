use crate::project::nodes::{LavaArray, LavaAttribute, LavaFunction};

pub struct ProjectNodes {
    pub functions: Vec<LavaFunction>,
    pub attributes: Vec<LavaAttribute>,
    pub arrays: Vec<LavaArray>,
}

pub struct Project {
    pub nodes: ProjectNodes,
}

impl ProjectNodes {
    pub fn new() -> ProjectNodes {
        ProjectNodes {
            functions: Vec::new(),
            attributes: Vec::new(),
            arrays: Vec::new(),
        }
    }

    pub fn get_function(&self, handle_name: String) -> Option<&LavaFunction> {
        self.functions.iter().find(|f| f.handle == handle_name)
    }

    pub fn get_array(&self, name: String) -> Option<&LavaArray> {
        self.arrays.iter().find(|a| a.name == name)
    }

    pub fn get_attribute(&self, name: String) -> Option<&LavaAttribute> {
        self.attributes.iter().find(|a| a.name == name)
    }
}
