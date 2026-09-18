/**
 * A struct that holds a string for a the span of the program
 */
#[derive(Clone, Debug)]
pub struct StringCache {
    entity: Vec<String>,
}

impl StringCache {
    pub fn new() -> StringCache {
        StringCache { entity: Vec::new() }
    }

    pub fn push(&mut self, content: String) {
        self.entity.push(content);
    }

    pub fn to_string(&self) -> String {
        self.entity.join("") // ITS A WORD ITS NOT SEPERATE
    }

    pub fn clear(&mut self) {
        self.entity.clear();
    }

    pub fn is_empty(&self) -> bool {
        self.entity.is_empty()
    }

    pub fn equals_string(&self, text: &'static str) -> bool {
        self.to_string() == text.to_string()
    }
}
