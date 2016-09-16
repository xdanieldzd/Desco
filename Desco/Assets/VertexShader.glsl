#version 330

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec3 in_normal;
layout(location = 2) in vec3 in_unknown;
layout(location = 3) in vec4 in_color;
layout(location = 4) in vec2 in_texCoord;

out vec3 normal;
out vec4 color;
out vec2 texCoord;

uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;

uniform float timer;
uniform vec2 texCoord_offset;
uniform mat4 node_matrix;

void main(void)
{
    normal = (modelview_matrix * vec4(in_normal, 0)).xyz;
    color = in_color * 2.0;
    texCoord = in_texCoord + (texCoord_offset * timer);

    gl_Position = projection_matrix * modelview_matrix * node_matrix * vec4(in_position, 1);
}
