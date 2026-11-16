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
    elemento("estimacion").textContent =
        `Comprar costaría unos ${formatoPesos.format(importe + comision)}, `
        + `de los cuales ${formatoPesosConCentavos.format(comision)} son comisión.`;
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

retomarPartidaGuardada();
setInterval(actualizar, CONFIGURACION.intervaloDeActualizacionMs);