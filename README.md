# app1 – Agencia de Viajes (ASP.NET MVC, C#, PostgreSQL)

Esta aplicación es una agencia de viajes peruana desarrollada en ASP.NET MVC con autenticación Individual y PostgreSQL. Permite a los usuarios explorar viajes, reservar, gestionar su cuenta y a los administradores controlar el catálogo, reservas y publicaciones en redes sociales.

## Características principales

- Catálogo de viajes por departamentos
- Reservas y gestión de carrito
- Autenticación y roles (Cliente/Admin)
- CRUD de viajes y departamentos (Admin)

## Estructura recomendada

- Modelos: Viaje, Departamento, Cliente, Reserva
- Controladores y vistas para público, clientes y admin
- Área Admin con dashboard y CRUD

## Configuración

1. Configura la cadena de conexión a PostgreSQL en `appsettings.json`.
2. Ejecuta las migraciones con `dotnet ef database update`.
3. Personaliza los modelos y vistas según tus necesidades.

## Despliegue

- Repositorio: https://github.com/scett1c0618/app1
- Despliegue recomendado: Render.com

