pub mod lexer;
pub mod nodes;
pub mod os_types;

#[cfg(test)]
mod tests {
    use basalt_common::ProjectMetadata;

    use crate::lexer::common::LavaLexer;

    #[test]
    fn lexer_project_test() {
        let mut lava_lexer = LavaLexer::new();
        let project_meta = ProjectMetadata {
            file_name: "build.lava",
        };

        lava_lexer.run(project_meta.fetch_file());

        for node in lava_lexer.nodes {
            println!("NODE NAME: {}, TYPE: {:#?}", node.value, node.node_type)
        }
    }
}
