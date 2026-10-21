#version 330 core

// Atributele vertexului (din VBO, descrise in VAO)
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

// Uniforme din laboratorul 2
uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 uOffset;

// Uniforme noi (laboratorul 3)
uniform float uTime;           // timpul animatiei, in secunde (sta pe loc cand aplicatia e in pauza)
uniform float uWaveAmplitude;  // amplitudinea undei; 0 = unda oprita (tasta W)
uniform float uWaveFrequency;  // cat de "deasa" este unda
uniform int uColorMode;        // 0 = culoarea din VBO, 1 = culoare calculata din pozitie (tasta C)

// Iesire catre fragment shader (valoarea va fi interpolata la rasterizare)
out vec3 vColor;

void main()
{
    vec3 position = aPosition;

    // P5a: amplitudinea zero opreste unda fara a schimba datele din VBO.
    position.y += uWaveAmplitude * sin(uWaveFrequency * position.x + uTime);

    // P5c: culoarea poate veni din VBO sau poate fi calculata din pozitia initiala.
    if (uColorMode == 1)
    {
        vColor = aPosition * 0.5 + 0.5;
    }
    else
    {
        vColor = aColor;
    }

    gl_Position = uProjection * uView * vec4(position + uOffset, 1.0);
}
