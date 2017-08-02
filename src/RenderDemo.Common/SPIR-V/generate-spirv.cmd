glslangvalidator.exe -V simple-vertex.glsl -o simple-vertex.spv -S vert
glslangvalidator.exe -V simple-frag.glsl -o simple-frag.spv -S frag

glslangvalidator.exe -V textured-vertex.glsl -o textured-vertex.spv -S vert
glslangvalidator.exe -V lit-frag.glsl -o lit-frag.spv -S frag

glslangvalidator.exe -V shadowmap-vertex.glsl -o shadowmap-vertex.spv -S vert
glslangvalidator.exe -V shadowmap-frag.glsl -o shadowmap-frag.spv -S frag

glslangvalidator.exe -V forward_mtl-vertex.glsl -o forward_mtl-vertex.spv -S vert
glslangvalidator.exe -V forward_mtl-frag.glsl -o forward_mtl-frag.spv -S frag

glslangvalidator.exe -V shadow-vertex.glsl -o shadow-vertex.spv -S vert
glslangvalidator.exe -V shadow-frag.glsl -o shadow-frag.spv -S frag

glslangvalidator.exe -V wireframe-vertex.glsl -o wireframe-vertex.spv -S vert
glslangvalidator.exe -V wireframe-frag.glsl -o wireframe-frag.spv -S frag

glslangvalidator.exe -V skybox-vertex.glsl -o skybox-vertex.spv -S vert
glslangvalidator.exe -V skybox-frag.glsl -o skybox-frag.spv -S frag

glslangvalidator.exe -V simple-2d-vertex.glsl -o simple-2d-vertex.spv -S vert
glslangvalidator.exe -V simple-2d-frag.glsl -o simple-2d-frag.spv -S frag

glslangvalidator.exe -V instanced-simple-vertex.glsl -o instanced-simple-vertex.spv -S vert
glslangvalidator.exe -V instanced-simple-frag.glsl -o instanced-simple-frag.spv -S frag

glslangvalidator.exe -V geometry-vertex.glsl -o geometry-vertex.spv -S vert
glslangvalidator.exe -V geometry-frag.glsl -o geometry-frag.spv -S frag

glslangvalidator.exe -V billboard-geometry.glsl -o billboard-geometry.spv -S geom
glslangvalidator.exe -V cube-geometry.glsl -o cube-geometry.spv -S geom
