use basalt_common::ProjectMetadata;

use crate::lexer::LavaNode;

pub mod lexer;
pub mod os_types;
pub mod parser;

pub struct LanguagePipeline {
    pub project_meta: ProjectMetadata,
    pub nodes: Option<Vec<LavaNode>>,
}

impl LanguagePipeline {
    pub fn new(metdata: ProjectMetadata) -> LanguagePipeline {
        LanguagePipeline {
            project_meta: metdata,
            nodes: None,
        }
    }

    pub fn into_lexer(&mut self) {
        let mut lava_lexer = lexer::common::LavaLexer::new();
        lava_lexer.run(self.project_meta.fetch_file());
        self.nodes = Some(lava_lexer.nodes);
    }

    pub fn into_parser(self) {
        let lava_parser = parser::common::LavaParser::new(
            self.nodes
                .expect("into_lexer() was not called before into_parser()"),
        );
        lava_parser.run();
    }
}

#[cfg(test)]
mod tests {
    use basalt_common::ProjectMetadata;

    use crate::LanguagePipeline;

    #[test]
    fn lexer_project_test() {
        let mut pipeline = LanguagePipeline::new(ProjectMetadata {
            file_name: "build.lava",
        });
        pipeline.into_lexer();

        for node in pipeline.nodes.expect("nodes are empty") {
            println!("{:#?}", node);
        }
    }
}
