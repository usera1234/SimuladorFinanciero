"use strict";

// =========================================================================
// Simulador de inversiones — interfaz
//
// Organización del archivo:
//   1. Configuración
//   2. Comunicación con la API
//   3. Formato de números
//   4. Estado de la interfaz
//   5. Dibujo de cada sección
//   6. Acciones del jugador
//   7. Arranque
//
// Todas las rutas son relativas ("/partidas"): la página funciona desde
// cualquier dispositivo que la abra, siempre que el servidor sea accesible.
// =========================================================================


// ----- 1. Configuración --------------------------------------------------

const CONFIGURACION = {
    // Cada cuánto se consulta la partida. Con el reloj perezoso, esto es lo que hace avanzar el tiempo en pantalla.
    intervaloDeActualizacionMs: 1000,

    // Dónde guarda el navegador la partida en curso, para retomarla al recargar.
    claveDePartidaGuardada: "idPartida",

    // Debe coincidir con Billetera.TasaDeComision en el servidor. Solo se usa para la estimación previa.
    tasaDeComision: 0.005,

    // Cantidad máxima de días que se piden para el gráfico. Más puntos = más detalle y más datos por consulta.
    puntosDelHistorial: 300,

    // Alto del gráfico de la billetera, en píxeles.
    altoDelGrafico: 240,
};

const NOMBRES_DE_TIPOS = {
    Ahorro: "Ahorro",
    Moneda: "Moneda",
    Accion: "Acción",
    Cripto: "Cripto",
};


// ----- 2. Comunicación con la API ----------------------------------------

// Error devuelto por la API, con su código HTTP (por ejemplo, 404 si la partida no existe).
class ErrorDeApi extends Error {
    constructor(mensaje, estado) {
        super(mensaje);
        this.estado = estado;
    }
}

// Envía un pedido y devuelve la respuesta convertida desde JSON.
// Si la API responde con error, lanza un ErrorDeApi con el mensaje del servidor.
async function pedir(ruta, metodo = "GET", cuerpo = null) {
    const opciones = { method: metodo, headers: {} };

    if (cuerpo !== null) {
        opciones.headers["Content-Type"] = "application/json";
        opciones.body = JSON.stringify(cuerpo);
    }

    const respuesta = await fetch(ruta, opciones);
    const texto = await respuesta.text();
    const datos = texto ? JSON.parse(texto) : null;

    if (!respuesta.ok) {
        const mensaje = datos?.detail ?? datos?.title ?? `Error ${respuesta.status}`;
        throw new ErrorDeApi(mensaje, respuesta.status);
    }

    return datos;
}

// Todas las rutas de la API, en un solo lugar.
const api = {
    crear: () => pedir("/partidas", "POST"),
    obtener: (id) => pedir(`/partidas/${id}`),
    pausar: (id) => pedir(`/partidas/${id}/pausar`, "POST"),
    reanudar: (id) => pedir(`/partidas/${id}/reanudar`, "POST"),
    cambiarVelocidad: (id, velocidad) => pedir(`/partidas/${id}/velocidad`, "PUT", { velocidad }),
    comprar: (id, activo, cantidad) => pedir(`/partidas/${id}/comprar`, "POST", { activo, cantidad }),
    vender: (id, activo, cantidad) => pedir(`/partidas/${id}/vender`, "POST", { activo, cantidad }),
    historial: (id, puntos) => pedir(`/partidas/${id}/historial?puntos=${puntos}`),
};


// ----- 3. Formato de números ---------------------------------------------

const formatoPesos = new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: "ARS",
    maximumFractionDigits: 0,
});

const formatoPesosConCentavos = new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: "ARS",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
});

// Los precios muy bajos (como el plazo fijo, que arranca en $1) necesitan más decimales.
function formatearPrecio(valor) {
    const decimales = valor < 10 ? 4 : valor < 1000 ? 2 : 0;
    return valor.toLocaleString("es-AR", {
        style: "currency",
        currency: "ARS",
        minimumFractionDigits: decimales,
        maximumFractionDigits: decimales,
    });
}

// +12,50 % / −3,20 % / 0,00 %. El signo se muestra siempre, para no depender solo del color.
function formatearPorcentaje(valor) {
    const signo = valor > 0 ? "+" : valor < 0 ? "−" : "";
    const numero = Math.abs(valor).toLocaleString("es-AR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return `${signo}${numero} %`;
}

function formatearCantidad(valor) {
    return valor.toLocaleString("es-AR", { maximumFractionDigits: 8 });
}

function claseSegunSigno(valor) {
    return valor > 0 ? "ganancia" : valor < 0 ? "perdida" : "";
}


// ----- 4. Estado de la interfaz ------------------------------------------

let idPartida = null;        // Partida en juego, o null.
let ultimaPartida = null;    // Último estado recibido de la API.
let consultando = false;     // Evita superponer consultas si el servidor tarda.
let selectorPreparado = false;
let ultimoHistorial = null;  // Último historial recibido, para redibujar el gráfico sin volver a pedirlo.
let tickDelHistorial = -1;   // Día del último historial pedido: si no cambió, no se vuelve a pedir.
let pidiendoHistorial = false;

function elemento(id) {
    return document.getElementById(id);
}

function guardarPartida(id) {
    idPartida = id;
    localStorage.setItem(CONFIGURACION.claveDePartidaGuardada, id);
}

function olvidarPartida() {
    idPartida = null;
    ultimaPartida = null;
    selectorPreparado = false;
    ultimoHistorial = null;
    tickDelHistorial = -1;
    dibujarGraficoDeBilletera();
    localStorage.removeItem(CONFIGURACION.claveDePartidaGuardada);
    mostrarPantalla(false);
}

// Muestra la pantalla de juego o la de inicio.
function mostrarPantalla(enJuego) {
    elemento("pantalla-inicio").hidden = enJuego;
    elemento("pantalla-juego").hidden = !enJuego;
    elemento("controles-tiempo").hidden = !enJuego;
}

// Error general (conexión, partida inexistente). Se borra en la siguiente consulta exitosa.
function mostrarError(mensaje) {
    const caja = elemento("mensaje-error");
    caja.textContent = mensaje ?? "";
    caja.hidden = mensaje === null;
}

// Resultado de una compra o venta. No se borra con las consultas periódicas.
function mostrarMensajeDeOperacion(mensaje, esError) {
    const caja = elemento("mensaje-operacion");
    caja.textContent = mensaje;
    caja.classList.toggle("es-error", esError);
    caja.hidden = false;
}

function manejarError(error) {
    if (error instanceof ErrorDeApi && error.estado === 404) {
        olvidarPartida();
        mostrarError("La partida ya no existe (puede que la aplicación se haya reiniciado). Empezá una nueva.");
        return;
    }

    mostrarError(error.message || "No se pudo conectar con el simulador.");
}


// ----- 5. Dibujo de cada sección -----------------------------------------

// Crea un elemento con texto y, opcionalmente, una clase. Usa textContent para no interpretar HTML.
function crear(etiqueta, texto = "", clase = "") {
    const nuevo = document.createElement(etiqueta);
    nuevo.textContent = texto;
    if (clase) {
        nuevo.className = clase;
    }
    return nuevo;
}

function mostrarPartida(partida) {
    ultimaPartida = partida;
    const { mundo, billetera } = partida;

    mostrarTiempo(mundo);
    mostrarPatrimonio(mundo, billetera);
    mostrarMercado(mundo.activos);
    prepararSelectorDeActivos(mundo.activos);
    mostrarBilletera(billetera);
    mostrarNoticias(mundo.ultimosEventos);
    mostrarEconomia(mundo.factores);
    mostrarOperaciones(billetera.ultimasOperaciones);
    actualizarEstimacion();
    actualizarHistorial(mundo.tick);
}

function mostrarTiempo(mundo) {
    elemento("dia").textContent = `Día ${mundo.tick.toLocaleString("es-AR")}`;
    elemento("boton-reanudar").hidden = !mundo.estaPausado;
    elemento("boton-pausar").hidden = mundo.estaPausado;

    // No se pisa el selector mientras el jugador lo está usando.
    const selector = elemento("selector-velocidad");
    if (document.activeElement !== selector) {
        selector.value = String(mundo.velocidad);
    }
}

function mostrarPatrimonio(mundo, billetera) {
    elemento("valor-total").textContent = formatoPesos.format(billetera.valorTotal);
    elemento("valor-real").textContent = formatoPesos.format(billetera.valorReal);
    elemento("inflacion").textContent = formatearPorcentaje(mundo.inflacionAcumuladaPorcentual);

    const nominal = elemento("ganancia-nominal");
    nominal.textContent = formatearPorcentaje(billetera.gananciaNominalPorcentual);
    nominal.className = claseSegunSigno(billetera.gananciaNominalPorcentual);

    const real = elemento("ganancia-real");
    real.textContent = formatearPorcentaje(billetera.gananciaRealPorcentual);
    real.className = claseSegunSigno(billetera.gananciaRealPorcentual);
}

function mostrarMercado(activos) {
    const escala = calcularEscalaDeMinigraficos();

    const filas = activos.map((activo) => {
        const fila = document.createElement("tr");

        // El nombre es un botón: así se puede elegir con el teclado y con el dedo, no solo con el mouse.
        const celdaNombre = document.createElement("td");
        const boton = crear("button", activo.nombre, "enlace");
        boton.type = "button";
        boton.dataset.activo = activo.nombre;
        celdaNombre.append(boton);

        fila.append(
            celdaNombre,
            crear("td", NOMBRES_DE_TIPOS[activo.tipo] ?? activo.tipo, "col-tipo"),
            crear("td", formatearPrecio(activo.precio), "numero"),
            crear("td", formatearPorcentaje(activo.variacionDesdeElInicio), `numero ${claseSegunSigno(activo.variacionDesdeElInicio)}`),
        );

        const celdaMinigrafico = crear("td", "", "col-minigrafico");
        celdaMinigrafico.append(crearMinigrafico(activo.nombre, escala));
        fila.append(celdaMinigrafico);
        return fila;
    });

    elemento("cuerpo-mercado").replaceChildren(...filas);
}

// Las opciones del formulario se arman una sola vez: los activos no cambian durante la partida.
function prepararSelectorDeActivos(activos) {
    if (selectorPreparado) {
        return;
    }

    const opciones = activos.map((activo) => {
        const opcion = crear("option", activo.nombre);
        opcion.value = activo.nombre;
        return opcion;
    });

    elemento("selector-activo").replaceChildren(...opciones);
    selectorPreparado = true;
}

function mostrarBilletera(billetera) {
    elemento("efectivo").textContent = formatoPesosConCentavos.format(billetera.efectivo);

    const filas = billetera.posiciones.map((posicion) => {
        const fila = document.createElement("tr");
        fila.append(
            crear("td", posicion.activo),
            crear("td", formatearCantidad(posicion.cantidad), "numero"),
            crear("td", formatoPesos.format(posicion.valor), "numero"),
        );
        return fila;
    });

    elemento("cuerpo-posiciones").replaceChildren(...filas);
    elemento("tabla-posiciones").hidden = filas.length === 0;
    elemento("billetera-vacia").hidden = filas.length > 0;
}

function mostrarNoticias(eventos) {
    const items = eventos.map((evento) => {
        const item = document.createElement("li");
        const impacto = crear("strong", formatearPorcentaje(evento.impactoPorcentual), claseSegunSigno(evento.impactoPorcentual));
        item.append(
            crear("span", `Día ${evento.tick.toLocaleString("es-AR")}`, "cuando"),
            `${evento.empresa}: ${evento.evento} `,
            impacto,
        );
        return item;
    });

    elemento("lista-noticias").replaceChildren(...items);
    elemento("noticias-vacias").hidden = items.length > 0;
}

function mostrarEconomia(factores) {
    const contenido = factores.flatMap((factor) => {
        const esPorcentaje = factor.nombre.includes("(%)");
        const valor = esPorcentaje
            ? `${factor.valor.toLocaleString("es-AR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })} %`
            : formatearPorcentaje(factor.valor).replace(" %", "");
        return [crear("dt", factor.nombre), crear("dd", valor)];
    });

    elemento("lista-factores").replaceChildren(...contenido);
}

function mostrarOperaciones(operaciones) {
    const items = operaciones.map((operacion) => {
        const verbo = operacion.tipo === "Compra" ? "Compraste" : "Vendiste";
        const texto = `${verbo} ${formatearCantidad(operacion.cantidad)} de ${operacion.activo} a ${formatearPrecio(operacion.precio)} `
            + `(comisión ${formatoPesosConCentavos.format(operacion.comision)})`;

        const item = document.createElement("li");
        item.append(crear("span", `Día ${operacion.tick.toLocaleString("es-AR")}`, "cuando"), texto);
        return item;
    });

    elemento("lista-operaciones").replaceChildren(...items);
    elemento("operaciones-vacias").hidden = items.length > 0;
}


// ----- 5b. Gráfico de la billetera ---------------------------------------
// Se dibuja con SVG, sin bibliotecas: cada línea es una "polyline" con un punto por día.

const ESPACIO_DE_SVG = "http://www.w3.org/2000/svg";

// Lugar que se deja alrededor del dibujo para las etiquetas de los ejes, en píxeles.
const MARGENES_DEL_GRAFICO = { arriba: 12, derecha: 12, abajo: 28, izquierda: 72 };

// Lo que hace falta para el efecto de pasar el mouse: dónde está cada día en pantalla.
let escalaDelGrafico = null;

// Igual que crear(), pero para elementos SVG, que necesitan su propio espacio de nombres.
function crearSvg(etiqueta, atributos = {}, texto = "") {
    const nuevo = document.createElementNS(ESPACIO_DE_SVG, etiqueta);
    for (const [nombre, valor] of Object.entries(atributos)) {
        nuevo.setAttribute(nombre, valor);
    }
    nuevo.textContent = texto;
    return nuevo;
}

// Pide el historial solo si cambió el día (o si se pide forzarlo, por ejemplo después de operar).
async function actualizarHistorial(tick, forzar = false) {
    if (idPartida === null || pidiendoHistorial || (!forzar && tick === tickDelHistorial)) {
        return;
    }

    pidiendoHistorial = true;
    try {
        ultimoHistorial = await api.historial(idPartida, CONFIGURACION.puntosDelHistorial);
        tickDelHistorial = tick;
        dibujarGraficoDeBilletera();

        // Los minigráficos del mercado usan el mismo historial.
        if (ultimaPartida !== null) {
            mostrarMercado(ultimaPartida.mundo.activos);
        }
    } catch (error) {
        // El gráfico es secundario: si falla, el resto de la pantalla sigue funcionando.
        console.warn("No se pudo obtener el historial:", error);
    } finally {
        pidiendoHistorial = false;
    }
}

function dibujarGraficoDeBilletera() {
    const svg = elemento("grafico-billetera");
    const historial = ultimoHistorial;
    const hayDatos = historial !== null && historial.ticks.length >= 2;

    // En SVG no existe la propiedad .hidden (es solo de HTML): hay que poner o sacar el atributo.
    svg.toggleAttribute("hidden", !hayDatos);
    elemento("grafico-vacio").hidden = hayDatos;
    elemento("grafico-detalle").hidden = !hayDatos;
    escalaDelGrafico = null;

    if (!hayDatos) {
        svg.replaceChildren();
        return;
    }

    // El dibujo usa el ancho real en pantalla, así el texto no se estira.
    const ancho = svg.clientWidth || 600;
    const alto = CONFIGURACION.altoDelGrafico;
    svg.setAttribute("viewBox", `0 0 ${ancho} ${alto}`);

    const { ticks, valorTotal, valorReal } = historial;
    const capitalInicial = ultimaPartida?.billetera.capitalInicial ?? valorTotal[0];

    // Rango vertical: todos los valores y el capital inicial, redondeado a valores "lindos" para la grilla.
    const todos = [...valorTotal, ...valorReal, capitalInicial];
    const { minimo, maximo, paso } = calcularEscalaRedonda(Math.min(...todos), Math.max(...todos));

    const izquierda = MARGENES_DEL_GRAFICO.izquierda;
    const derecha = ancho - MARGENES_DEL_GRAFICO.derecha;
    const arriba = MARGENES_DEL_GRAFICO.arriba;
    const abajo = alto - MARGENES_DEL_GRAFICO.abajo;

    // Escalas: convierten un día y un valor en pesos a una posición en pantalla.
    const primerDia = ticks[0];
    const ultimoDia = ticks[ticks.length - 1];
    const x = (tick) => izquierda + (tick - primerDia) / (ultimoDia - primerDia) * (derecha - izquierda);
    const y = (valor) => abajo - (valor - minimo) / (maximo - minimo) * (abajo - arriba);

    const contenido = [];

    // Grilla horizontal: una línea por cada "paso", con su valor a la izquierda.
    for (let valor = minimo; valor <= maximo + paso / 2; valor += paso) {
        const altura = y(valor);
        contenido.push(
            crearSvg("line", { class: "grilla", x1: izquierda, x2: derecha, y1: altura, y2: altura }),
            crearSvg("text", { class: "eje", x: izquierda - 8, y: altura, "text-anchor": "end", "dominant-baseline": "middle" },
                formatearEnMillones(valor)),
        );
    }

    // Primer y último día, debajo del dibujo.
    contenido.push(
        crearSvg("text", { class: "eje", x: izquierda, y: alto - 6, "text-anchor": "start" }, `Día ${primerDia.toLocaleString("es-AR")}`),
        crearSvg("text", { class: "eje", x: derecha, y: alto - 6, "text-anchor": "end" }, `Día ${ultimoDia.toLocaleString("es-AR")}`),
    );

    // Línea punteada con la plata del inicio: lo que está arriba es ganancia, lo de abajo, pérdida.
    const alturaInicial = y(capitalInicial);
    contenido.push(crearSvg("line", { class: "linea-inicial", x1: izquierda, x2: derecha, y1: alturaInicial, y2: alturaInicial }));

    // Primero la real y después la nominal, para que la nominal quede encima si se cruzan.
    contenido.push(
        crearPolilinea(ticks, valorReal, x, y, "linea-real"),
        crearPolilinea(ticks, valorTotal, x, y, "linea-nominal"),
    );

    // Elementos del efecto de pasar el mouse: empiezan ocultos.
    const guia = crearSvg("line", { class: "guia", y1: arriba, y2: abajo, visibility: "hidden" });
    const marcadorNominal = crearSvg("circle", { class: "marcador linea-nominal", r: 4.5, visibility: "hidden" });
    const marcadorReal = crearSvg("circle", { class: "marcador linea-real", r: 4.5, visibility: "hidden" });
    contenido.push(guia, marcadorReal, marcadorNominal);

    svg.replaceChildren(...contenido);
    escalaDelGrafico = { x, y, guia, marcadorNominal, marcadorReal };
    mostrarDetalleDelGrafico(null);
}

// Elige un paso redondo (1, 2, 2,5 o 5 por una potencia de 10) para que la grilla tenga unas 4 líneas,
// y estira el rango hasta múltiplos de ese paso. Ejemplo: de 883.620 a 1.320.000 → de 800.000 a 1.400.000, de a 200.000.
function calcularEscalaRedonda(minimoReal, maximoReal) {
    // Al menos un 2 % del valor: si no, con la plata quieta la grilla mostraría diferencias de centavos.
    const rango = Math.max(maximoReal - minimoReal, maximoReal * 0.02);
    const pasoAproximado = rango / 3;
    const potencia = 10 ** Math.floor(Math.log10(pasoAproximado));
    const paso = [1, 2, 2.5, 5, 10].map((m) => m * potencia).find((p) => p >= pasoAproximado);

    return {
        minimo: Math.floor(minimoReal / paso) * paso,
        maximo: Math.ceil(maximoReal / paso) * paso,
        paso,
    };
}

// "$1,2 M", "$0,85 M": todos los valores del eje en la misma unidad, para poder compararlos de un vistazo.
function formatearEnMillones(valor) {
    return `$${(valor / 1_000_000).toLocaleString("es-AR", { maximumFractionDigits: 3 })} M`;
}

// Une los puntos (día, valor) en una sola línea.
function crearPolilinea(ticks, valores, x, y, clase) {
    const puntos = ticks.map((tick, i) => `${x(tick).toFixed(1)},${y(valores[i]).toFixed(1)}`).join(" ");
    return crearSvg("polyline", { class: clase, points: puntos });
}

// Muestra los valores de un día debajo del gráfico. Sin día, muestra el último.
function mostrarDetalleDelGrafico(indice) {
    const { ticks, valorTotal, valorReal } = ultimoHistorial;
    const i = indice ?? ticks.length - 1;

    elemento("grafico-detalle").textContent =
        `Día ${ticks[i].toLocaleString("es-AR")}: ${formatoPesos.format(valorTotal[i])} en pesos, `
        + `que equivalen a ${formatoPesos.format(valorReal[i])} de cuando empezaste.`;
}

// Al pasar el mouse (o el dedo), busca el día más cercano y lo marca.
function seguirPunteroEnElGrafico(evento) {
    if (escalaDelGrafico === null) {
        return;
    }

    const { x, y, guia, marcadorNominal, marcadorReal } = escalaDelGrafico;
    const { ticks, valorTotal, valorReal } = ultimoHistorial;

    // Como el viewBox mide lo mismo que el gráfico en pantalla, offsetX ya está en las unidades del dibujo.
    let masCercano = 0;
    for (let i = 1; i < ticks.length; i++) {
        if (Math.abs(x(ticks[i]) - evento.offsetX) < Math.abs(x(ticks[masCercano]) - evento.offsetX)) {
            masCercano = i;
        }
    }

    const posicion = x(ticks[masCercano]);
    guia.setAttribute("x1", posicion);
    guia.setAttribute("x2", posicion);
    marcadorNominal.setAttribute("cx", posicion);
    marcadorNominal.setAttribute("cy", y(valorTotal[masCercano]));
    marcadorReal.setAttribute("cx", posicion);
    marcadorReal.setAttribute("cy", y(valorReal[masCercano]));

    for (const parte of [guia, marcadorNominal, marcadorReal]) {
        parte.setAttribute("visibility", "visible");
    }

    mostrarDetalleDelGrafico(masCercano);
}

function soltarPunteroDelGrafico() {
    if (escalaDelGrafico === null) {
        return;
    }

    for (const parte of [escalaDelGrafico.guia, escalaDelGrafico.marcadorNominal, escalaDelGrafico.marcadorReal]) {
        parte.setAttribute("visibility", "hidden");
    }

    mostrarDetalleDelGrafico(null);
}



// ----- 5c. Minigráficos del mercado --------------------------------------
// Una línea chiquita por activo, con la variación desde el inicio.

// Tamaño de cada minigráfico, en píxeles.
const ANCHO_DEL_MINIGRAFICO = 96;
const ALTO_DEL_MINIGRAFICO = 28;

// Todos los minigráficos usan la MISMA escala: la mayor variación de cualquier activo.
// Si cada uno usara la suya, el plazo fijo se vería tan movido como Bitcoin y el gráfico mentiría.
function calcularEscalaDeMinigraficos() {
    if (ultimoHistorial === null || ultimoHistorial.ticks.length < 2) {
        return null;
    }

    let mayorVariacion = 0;
    for (const serie of ultimoHistorial.activos) {
        const precioInicial = serie.precios[0];
        for (const precio of serie.precios) {
            mayorVariacion = Math.max(mayorVariacion, Math.abs(precio / precioInicial - 1));
        }
    }

    // Si nada se movió todavía, se usa un 1 % para no dividir por cero.
    return mayorVariacion || 0.01;
}

function crearMinigrafico(nombreDelActivo, escala) {
    const serie = ultimoHistorial?.activos.find((s) => s.nombre === nombreDelActivo);
    if (escala === null || serie === undefined) {
        return crear("span");
    }

    const ancho = ANCHO_DEL_MINIGRAFICO;
    const alto = ALTO_DEL_MINIGRAFICO;
    const medio = alto / 2;   // altura del precio inicial (variación 0 %)
    const { precios } = serie;
    const precioInicial = precios[0];

    // Variación de cada día respecto del inicio, convertida a altura: arriba sube, abajo baja.
    const puntos = precios.map((precio, i) => {
        const x = i / (precios.length - 1) * ancho;
        const y = medio - (precio / precioInicial - 1) / escala * (medio - 2);
        return `${x.toFixed(1)},${y.toFixed(1)}`;
    }).join(" ");

    const variacionFinal = precios[precios.length - 1] / precioInicial - 1;

    // aria-hidden: el porcentaje de la columna anterior ya dice lo mismo para los lectores de pantalla.
    const svg = crearSvg("svg", { class: "minigrafico", viewBox: `0 0 ${ancho} ${alto}`, width: ancho, height: alto, "aria-hidden": "true" });
    svg.append(
        crearSvg("line", { class: "minigrafico-base", x1: 0, x2: ancho, y1: medio, y2: medio }),
        crearSvg("polyline", { points: puntos, class: claseSegunSigno(variacionFinal) }),
    );
    return svg;
}


// Muestra cuánto costaría la operación antes de hacerla, comisión incluida.
function actualizarEstimacion() {
    const nombre = elemento("selector-activo").value;
    const activo = ultimaPartida?.mundo.activos.find((a) => a.nombre === nombre);
    const cantidad = leerCantidad();

    elemento("ayuda-cantidad").textContent = activo?.tipo === "Accion"
        ? "Las acciones se compran por unidades enteras."
        : "Podés comprar una fracción, por ejemplo 0,5.";

    if (!activo || !(cantidad > 0)) {
        elemento("estimacion").textContent = "";
        return;
    }

    const importe = activo.precio * cantidad;
    const comision = importe * CONFIGURACION.tasaDeComision;
    const tenencia = ultimaPartida.billetera.posiciones.find((p) => p.activo === activo.nombre)?.cantidad ?? 0;

    // No sabemos qué botón va a tocar el jugador, así que se muestran los dos casos.
    // La comisión se suma al comprar y se resta al vender.
    let texto = `Comprar te costaría unos ${formatoPesos.format(importe + comision)}`;
    if (tenencia > 0) {
        texto += `; vender te daría unos ${formatoPesos.format(importe - comision)} (tenés ${formatearCantidad(tenencia)})`;
    }
    texto += `. Comisión: ${formatoPesosConCentavos.format(comision)}.`;
    elemento("estimacion").textContent = texto;
}


// ----- 6. Acciones del jugador -------------------------------------------

// Acepta coma o punto como separador decimal ("0,5" o "0.5").
function leerCantidad() {
    const texto = elemento("cantidad").value.trim().replace(",", ".");
    return texto === "" ? NaN : Number(texto);
}

// Ejecuta una acción que devuelve la partida y la muestra; si falla, muestra el error.
async function ejecutar(accion) {
    try {
        const partida = await accion();
        mostrarError(null);
        mostrarPartida(partida);
        return true;
    } catch (error) {
        manejarError(error);
        return false;
    }
}

async function crearPartida() {
    try {
        const partida = await api.crear();
        guardarPartida(partida.id);
        mostrarPantalla(true);
        mostrarError(null);
        mostrarPartida(partida);
    } catch (error) {
        manejarError(error);
    }
}

function empezarDeNuevo() {
    const confirmado = window.confirm("¿Empezar una partida nueva? La partida actual se va a perder.");
    if (!confirmado) {
        return;
    }

    olvidarPartida();
    elemento("mensaje-operacion").hidden = true;
    elemento("cantidad").value = "";
}

async function operar(evento) {
    evento.preventDefault();

    const tipo = evento.submitter?.value ?? "comprar";
    const activo = elemento("selector-activo").value;
    const cantidad = leerCantidad();

    if (!(cantidad > 0)) {
        mostrarMensajeDeOperacion("Ingresá una cantidad mayor que cero.", true);
        return;
    }

    try {
        const partida = tipo === "comprar"
            ? await api.comprar(idPartida, activo, cantidad)
            : await api.vender(idPartida, activo, cantidad);

        mostrarPartida(partida);
        elemento("cantidad").value = "";
        actualizarEstimacion();

        // El día no cambió, pero la billetera sí (al menos por la comisión): se redibuja el gráfico.
        actualizarHistorial(partida.mundo.tick, true);

        const verbo = tipo === "comprar" ? "Compraste" : "Vendiste";
        mostrarMensajeDeOperacion(`${verbo} ${formatearCantidad(cantidad)} de ${activo}.`, false);
    } catch (error) {
        if (error instanceof ErrorDeApi && error.estado === 404) {
            manejarError(error);
            return;
        }

        // Errores del jugador (efectivo insuficiente, fracciones de acciones, etc.): mensaje del servidor.
        mostrarMensajeDeOperacion(error.message, true);
    }
}

// Elegir un activo desde la tabla del mercado lo selecciona en el formulario.
function elegirActivoDesdeElMercado(evento) {
    const boton = evento.target.closest("button[data-activo]");
    if (!boton) {
        return;
    }

    elemento("selector-activo").value = boton.dataset.activo;
    actualizarEstimacion();
    elemento("cantidad").focus();
}

// Consulta periódica: pone al día el mundo en el servidor y la pantalla.
async function actualizar() {
    if (idPartida === null || consultando) {
        return;
    }

    consultando = true;
    try {
        await ejecutar(() => api.obtener(idPartida));
    } finally {
        consultando = false;
    }
}

// Si el navegador ya tenía una partida guardada, la retoma.
async function retomarPartidaGuardada() {
    const guardada = localStorage.getItem(CONFIGURACION.claveDePartidaGuardada);
    if (guardada === null) {
        return;
    }

    try {
        const partida = await api.obtener(guardada);
        idPartida = guardada;
        mostrarPantalla(true);
        mostrarPartida(partida);
    } catch {
        // La partida ya no existe (por ejemplo, se reinició la aplicación): se empieza de cero.
        localStorage.removeItem(CONFIGURACION.claveDePartidaGuardada);
    }
}


// ----- 7. Arranque -------------------------------------------------------

elemento("boton-crear").addEventListener("click", crearPartida);
elemento("boton-nueva").addEventListener("click", empezarDeNuevo);

elemento("boton-reanudar").addEventListener("click", () => ejecutar(() => api.reanudar(idPartida)));
elemento("boton-pausar").addEventListener("click", () => ejecutar(() => api.pausar(idPartida)));
elemento("selector-velocidad").addEventListener("change", (evento) =>
    ejecutar(() => api.cambiarVelocidad(idPartida, Number(evento.target.value))));

elemento("formulario-operacion").addEventListener("submit", operar);
elemento("selector-activo").addEventListener("change", actualizarEstimacion);
elemento("cantidad").addEventListener("input", actualizarEstimacion);
elemento("cuerpo-mercado").addEventListener("click", elegirActivoDesdeElMercado);

elemento("grafico-billetera").addEventListener("pointermove", seguirPunteroEnElGrafico);
elemento("grafico-billetera").addEventListener("pointerleave", soltarPunteroDelGrafico);
window.addEventListener("resize", dibujarGraficoDeBilletera);

retomarPartidaGuardada();
setInterval(actualizar, CONFIGURACION.intervaloDeActualizacionMs);