#version 330

in vec3 normal;
in vec4 color;
in vec2 texCoord;
out vec4 out_frag_color;

uniform sampler2D texture;

void main(void)
{
    out_frag_color = (color * texture2D(texture, texCoord));
}
