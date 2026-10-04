# Grupos de logros

Los grupos de Checkpoint permiten elegir un juego de la biblioteca, invitar amigos ya aceptados y avanzar en una lista compartida de logros. Cualquier miembro puede marcar objetivos completados y añadir objetivos; quien creó el grupo puede retirarlos. Las invitaciones solo funcionan entre amistades vigentes de Checkpoint.

La lista es un seguimiento compartido dentro de Checkpoint. Marcar un objetivo no desbloquea un logro en Steam ni RetroAchievements. Los logros ocultos no se incluyen automáticamente para evitar spoilers; puedes añadirlos como objetivo manual si el grupo decide revelarlos.

La base de datos conserva únicamente el nombre del grupo, el título del juego, el identificador de Steam opcional y el texto de los objetivos con su estado. No almacena carátulas ni notas privadas. Los datos se limitan a 200 objetivos por grupo, nombres de objetivo de hasta 250 caracteres y descripciones de hasta 2000 caracteres y un máximo de 150 kB de texto de logros por grupo. Row Level Security restringe grupos a sus integrantes y las invitaciones a sus destinatarios o integrantes.

Las migraciones `supabase/migrations/202610040006_checkpoint_groups.sql` y `supabase/migrations/202610040007_checkpoint_group_text_limit.sql` están aplicadas al proyecto Supabase de Checkpoint. Los cambios de la app son locales; para usar grupos, publica una versión nueva y los integrantes del grupo deben actualizarla.
