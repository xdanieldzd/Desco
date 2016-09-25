#version 330

in vec3 normal;
in vec4 color;
in vec2 texCoord;
out vec4 out_frag_color;

uniform sampler2D texture;

uniform float alpha_reference;
uniform bool alpha_doLessThan;

void main(void)
{
    vec4 tex_color = texture2D(texture, texCoord);
    out_frag_color = ((color * 2.0) * tex_color);
	if((alpha_doLessThan && out_frag_color.a < alpha_reference) || (!alpha_doLessThan && out_frag_color.a >= alpha_reference)) discard;
}
