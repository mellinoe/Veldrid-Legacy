glslangvalidator.exe -V simple-vertex.glsl -o simple-vertex.spv -S vert
glslangvalidator.exe -V simple-frag.glsl -o simple-frag.spv -S frag

glslangvalidator.exe -V textured-vertex.glsl -o textured-vertex.spv -S vert
glslangvalidator.exe -V lit-frag.glsl -o lit-frag.spv -S frag

glslangvalidator.exe -V shadowmap-vertex.glsl -o shadowmap-vertex.spv -S vert
glslangvalidator.exe -V shadowmap-frag.glsl -o shadowmap-frag.spv -S frag

glslangvalidator.exe -V forward_mtl-vertex.glsl -o forward_mtl-vertex.spv -S vert
glslangvalidator.exe -V forward_mtl-frag.glsl -o forward_mtl-frag.spv -S frag