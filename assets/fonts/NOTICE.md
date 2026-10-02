# unscii-16

Fonte bitmap 8×16 de viznut, distribuída em domínio público pelo projeto
[unscii](http://pelulamu.net/unscii/).

`unscii-16.hex` contém linhas `CODEPOINT:HEXBITS`; cada glifo tem 16 bytes,
um por linha, com o bit mais significativo representando o pixel à esquerda.
O frontend usa esse arquivo para desenhar o terminal e gerar o ícone do app.
