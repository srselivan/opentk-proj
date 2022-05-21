#version 330

out vec4 outputColor;

in vec2 TexCoords;

uniform sampler2D texture0;


void main()
{
    outputColor = mix(texture(texture0, TexCoords), texture(texture0, TexCoords), 0.2);
}