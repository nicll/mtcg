-- Table: public.cards

-- DROP TABLE public.cards;

CREATE TABLE public.cards
(
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    damage integer NOT NULL,
    element_type element_type NOT NULL,
    monster_type monster_type NOT NULL,
    CONSTRAINT cards_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE public.cards
    OWNER to postgres;

GRANT ALL ON TABLE public.cards TO mtcg_user;

GRANT ALL ON TABLE public.cards TO postgres;



-- Table: public.users

-- DROP TABLE public.users;

CREATE TABLE public.users
(
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    name character varying(8) COLLATE pg_catalog."default" NOT NULL,
    statusmsg character varying(80) COLLATE pg_catalog."default" NOT NULL,
    emoticon character varying(8) COLLATE pg_catalog."default" NOT NULL,
    coins smallint NOT NULL,
    elo smallint NOT NULL,
    wins smallint NOT NULL,
    losses smallint NOT NULL,
    pass_hash bytea NOT NULL,
    CONSTRAINT users_pkey PRIMARY KEY (id),
    CONSTRAINT users_name_key UNIQUE (name)
)

TABLESPACE pg_default;

ALTER TABLE public.users
    OWNER to postgres;

GRANT ALL ON TABLE public.users TO mtcg_user;

GRANT ALL ON TABLE public.users TO postgres;

GRANT SELECT ON TABLE public.users TO ro_user;

COMMENT ON TABLE public.users
    IS 'Stores users for MTCG.';



-- Table: public.stacks

-- DROP TABLE public.stacks;

CREATE TABLE public.stacks
(
    user_id uuid NOT NULL,
    card_id uuid NOT NULL,
    CONSTRAINT stacks_card_id_fkey FOREIGN KEY (card_id)
        REFERENCES public.cards (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID,
    CONSTRAINT stacks_user_id_fkey FOREIGN KEY (user_id)
        REFERENCES public.users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)

TABLESPACE pg_default;

ALTER TABLE public.stacks
    OWNER to postgres;

GRANT ALL ON TABLE public.stacks TO mtcg_user;

GRANT ALL ON TABLE public.stacks TO postgres;



-- Table: public.decks

-- DROP TABLE public.decks;

CREATE TABLE public.decks
(
    user_id uuid NOT NULL,
    card_id uuid NOT NULL,
    CONSTRAINT card_id FOREIGN KEY (card_id)
        REFERENCES public.cards (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT user_id FOREIGN KEY (user_id)
        REFERENCES public.users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE public.decks
    OWNER to postgres;

GRANT ALL ON TABLE public.decks TO mtcg_user;

GRANT ALL ON TABLE public.decks TO postgres;



-- Table: public.packages

-- DROP TABLE public.packages;

CREATE TABLE public.packages
(
    package_id uuid NOT NULL DEFAULT uuid_generate_v4(),
    price integer NOT NULL,
    card_ids uuid[] NOT NULL,
    CONSTRAINT packages_pkey PRIMARY KEY (package_id)
)

TABLESPACE pg_default;

ALTER TABLE public.packages
    OWNER to postgres;

GRANT ALL ON TABLE public.packages TO mtcg_user;

GRANT ALL ON TABLE public.packages TO postgres;



-- Table: public.store_entries

-- DROP TABLE public.store_entries;

CREATE TABLE public.store_entries
(
    card_id uuid NOT NULL,
    user_id uuid NOT NULL,
    reqs card_req[] NOT NULL,
    CONSTRAINT store_entries_pkey PRIMARY KEY (card_id),
    CONSTRAINT store_entries_card_id_fkey FOREIGN KEY (card_id)
        REFERENCES public.cards (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT store_entries_user_id_fkey FOREIGN KEY (user_id)
        REFERENCES public.users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE public.store_entries
    OWNER to postgres;

GRANT ALL ON TABLE public.store_entries TO mtcg_user;

GRANT ALL ON TABLE public.store_entries TO postgres;

