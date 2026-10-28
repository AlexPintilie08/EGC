#version 330 core

// Cele doua variante permit alegerea interpolarii normale sau flat din uniforma.
in vec3 vSmoothColor;
flat in vec3 vColor;
uniform int uFlatShading;

// Uniforma: o nuanta care se inmulteste cu culoarea (alb = culoarea ramane neschimbata)
uniform vec4 uTint;

// Culoarea finala a fragmentului
out vec4 FragColor;

void main()
{
    vec3 color = uFlatShading == 1 ? vColor : vSmoothColor;
    FragColor = vec4(color, 1.0) * uTint;
}
